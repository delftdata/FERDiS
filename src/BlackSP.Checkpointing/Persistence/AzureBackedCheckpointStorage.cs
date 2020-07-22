using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Extensions;
using BlackSP.Kernel.Models;
using BlackSP.Serialization.Extensions;
using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;

namespace BlackSP.Checkpointing.Persistence
{
    public class AzureBackedCheckpointStorage : ICheckpointStorage
    {

        private readonly RecyclableMemoryStreamManager _streamManager;


        public AzureBackedCheckpointStorage(RecyclableMemoryStreamManager streamManager)
        {
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        private async Task<BlobContainerClient> GetBlobContainerClientForCheckpoint(Guid id)
        {
            var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync($"{id}");
            await containerClient.CreateIfNotExistsAsync();
            return containerClient;
        }

        public async Task Delete(Guid id)
        {
            var blobContainerClient = await GetBlobContainerClientForCheckpoint(id);
            await blobContainerClient.DeleteAsync();
        }

        public async Task<Checkpoint> Retrieve(Guid id)
        {
            var blobContainerClient = await GetBlobContainerClientForCheckpoint(id);

            var blobs = blobContainerClient.GetBlobs(); //async version of GetBlobs is not actually async.. so keeping it synchronous for now
            var snapshots = new ConcurrentDictionary<string, ObjectSnapshot>();

            var blobToStreamTransform = new TransformBlock<BlobItem, Stream>(async blob =>
            {
                var client = blobContainerClient.GetBlobClient(blob.Name);
                var blobDownloadStream = _streamManager.GetStream();
                var response = await client.DownloadToAsync(blobDownloadStream);
                response.ThrowIfNotSuccessStatusCode();
                blobDownloadStream.Seek(0, SeekOrigin.Begin);
                return blobDownloadStream;
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

            var streamDeserializationAction = new ActionBlock<Stream>(stream =>
            {
                var downloadedObject = blobDownloadStream.BinaryDeserialize();
                var snapshot = downloadedObject as ObjectSnapshot ?? throw new Exception($"Downloaded blob did not contain expected ObjectSnapshot");
                snapshots.TryAdd(blob.Name, snapshot);
            });

            blobToStreamTransform.LinkTo(streamDeserializationAction, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });



            var parallelIteration = Parallel.ForEach(blobs, async blob =>
            {
                var client = blobContainerClient.GetBlobClient(blob.Name);
                using(var blobDownloadStream = _streamManager.GetStream())
                {
                    var response = await client.DownloadToAsync(blobDownloadStream);
                    response.ThrowIfNotSuccessStatusCode();
                    var downloadedObject = blobDownloadStream.BinaryDeserialize();
                    var snapshot = downloadedObject as ObjectSnapshot ?? throw new Exception($"Downloaded blob did not contain expected ObjectSnapshot");
                    snapshots.TryAdd(blob.Name, snapshot);
                }
            });



            if(!parallelIteration.IsCompleted)
            {
                throw new Exception($"Checkpoint \"{id}\" download failed to complete");

            }

            var checkpoint = new Checkpoint(id, snapshots);
            return checkpoint;
        }

        public async Task Store(Checkpoint checkpoint)
        {
            var blobContainerClient = await GetBlobContainerClientForCheckpoint(checkpoint.Id);

            var parallelIteration = Parallel.ForEach(checkpoint.Keys, async key =>
            {
                var blobClient = blobContainerClient.GetBlobClient(key);
                var snapshot = checkpoint.GetSnapshot(key);

                using (var snapshotUploadStream = _streamManager.GetStream())
                {
                    snapshot.BinarySerializeTo(snapshotUploadStream);
                    snapshotUploadStream.Seek(0, SeekOrigin.Begin);
                    var response = await blobClient.UploadAsync(snapshotUploadStream);
                }
            });

            if(!parallelIteration.IsCompleted)
            {
                throw new Exception($"Checkpoint \"{checkpoint.Id}\" upload failed to complete");
            }

            throw new NotImplementedException();
        }
    }
}
