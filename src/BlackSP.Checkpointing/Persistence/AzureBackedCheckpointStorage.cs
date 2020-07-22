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
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient($"{id}");
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

            var blobToStreamTransform = new TransformBlock<BlobItem, Tuple<string, MemoryStream>>(async blob =>
            {
                var client = blobContainerClient.GetBlobClient(blob.Name);
                var blobDownloadStream = _streamManager.GetStream();
                
                var response = await client.DownloadToAsync(blobDownloadStream);
                response.ThrowIfNotSuccessStatusCode();
                blobDownloadStream.Seek(0, SeekOrigin.Begin);
                return Tuple.Create(blob.Name, blobDownloadStream);
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

            var streamDeserializationAction = new ActionBlock<Tuple<string, MemoryStream>>(tuple =>
            {
                var (name, stream) = tuple;
                var downloadedObject = stream.BinaryDeserialize();
                var snapshot = downloadedObject as ObjectSnapshot ?? throw new Exception($"Downloaded blob did not contain expected ObjectSnapshot");
                snapshots.TryAdd(name, snapshot);
                stream.Dispose(); //ensure buffer is returned to manager
            });

            blobToStreamTransform.LinkTo(streamDeserializationAction, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            foreach (var blob in blobs) {
                await blobToStreamTransform.SendAsync(blob);
            }
            blobToStreamTransform.Complete();
            await streamDeserializationAction.Completion;

            var checkpoint = new Checkpoint(id, snapshots);
            return checkpoint;
        }

        public async Task Store(Checkpoint checkpoint)
        {
            var blobContainerClient = await GetBlobContainerClientForCheckpoint(checkpoint.Id);

            var snapshotSerializeTransform = new TransformBlock<string, Tuple<string, MemoryStream>>(async cpKey =>
            {
                var snapshot = checkpoint.GetSnapshot(cpKey);
                var snapshotUploadStream = _streamManager.GetStream();
                snapshot.BinarySerializeTo(snapshotUploadStream);
                snapshotUploadStream.Seek(0, SeekOrigin.Begin);
                return Tuple.Create(cpKey, snapshotUploadStream);
            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

            var streamUploadAction = new ActionBlock<Tuple<string, MemoryStream>>(async tuple =>
            {
                var (key, stream) = tuple;
                var blobClient = blobContainerClient.GetBlobClient(key);
                var response = await blobClient.UploadAsync(stream);
                stream.Dispose(); //ensure buffer is returned to manager
            });

            snapshotSerializeTransform.LinkTo(streamUploadAction, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            foreach(var cpKey in checkpoint.Keys)
            {
                await snapshotSerializeTransform.SendAsync(cpKey);
            }
            snapshotSerializeTransform.Complete();
            await streamUploadAction.Completion;
        }
    }
}
