using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Extensions;
using BlackSP.Serialization.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlackSP.Checkpointing.Persistence
{
    public class AzureBackedCheckpointStorage : ICheckpointStorage
    {

        private readonly ExecutionDataflowBlockOptions _blockOptions;
        private readonly DataflowLinkOptions _linkOptions;
        public AzureBackedCheckpointStorage()
        {
            _blockOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            _linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };
        }

        private BlobContainerClient GetBlobContainerClientForCheckpoints()
        {
            var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient($"checkpoints");
            containerClient.CreateIfNotExists();
            return containerClient;
        }

        public async Task Delete(Guid id)
        {
            var blobContainerClient = GetBlobContainerClientForCheckpoints();
            var blobsInCheckpoint = blobContainerClient.GetBlobs(prefix: $"id");
            foreach(var blob in blobsInCheckpoint)
            {
                var subBlob = blobContainerClient.GetBlobClient(blob.Name);
                if(await subBlob.ExistsAsync())
                {
                    await subBlob.DeleteAsync();
                }
            }
        }

        public async Task Store(Checkpoint checkpoint)
        {
            //ensure creation of blob container checkpoint will be stored in
            var blobContainerClient = GetBlobContainerClientForCheckpoints();

            await UploadCheckpointMetaData(checkpoint, blobContainerClient).ConfigureAwait(false);

            //Define dataflow for checkpoint storage
            var snapshotSerializeTransform = new TransformBlock<string, Tuple<string, MemoryStream>>(
                snapshotKey => SerializeSnapshotToStream(snapshotKey, checkpoint),
                _blockOptions
            );
            var streamUploadAction = new ActionBlock<Tuple<string, MemoryStream>>(
                async tuple => await UploadStreamToBlob(tuple, blobContainerClient)
            );
            snapshotSerializeTransform.LinkTo(streamUploadAction, _linkOptions);

            //Feed snapshotKeys to dataflow
            foreach (var snapshotKey in checkpoint.Keys)
            {
                await snapshotSerializeTransform.SendAsync(snapshotKey);
            }
            snapshotSerializeTransform.Complete();
            //Wait for upload completion
            await streamUploadAction.Completion;
        }

        

        public async Task<Checkpoint> Retrieve(Guid id)
        {
            //ensure existence of blob container checkpoint is stored in
            var blobContainerClient = GetBlobContainerClientForCheckpoints();
            
            var dependencies = await DownloadCheckpointMetaData(id, blobContainerClient);

            //Define dataflow for checkpoint retrieval
            var snapshots = new ConcurrentDictionary<string, ObjectSnapshot>();
            var blobDownloadToStreamTransform = new TransformBlock<BlobItem, Tuple<string, MemoryStream>>(
                async blob => await DownloadSerializedSnapshotToStream(blob, blobContainerClient), 
                _blockOptions
            );
            var streamDeserializationAction = new ActionBlock<Tuple<string, MemoryStream>>(
                tuple => DeserializeToDictionary(tuple, snapshots)
            );
            blobDownloadToStreamTransform.LinkTo(streamDeserializationAction, _linkOptions);
            //Feed blobs to dataflow
            var blobs = blobContainerClient.GetBlobs(BlobTraits.None, BlobStates.None, $"{id}"); //async version of GetBlobs is not actually async.. so keeping it synchronous for now
            foreach (var blob in blobs.Where(bi => !bi.Name.EndsWith("/meta"))) {
                await blobDownloadToStreamTransform.SendAsync(blob);
            }
            blobDownloadToStreamTransform.Complete();
            //Wait for completion of deserialization
            await streamDeserializationAction.Completion; 
            return new Checkpoint(id, snapshots, dependencies);
        }


        #region private dataflow methods
        private async Task<Tuple<string, MemoryStream>> DownloadSerializedSnapshotToStream(BlobItem blob, BlobContainerClient containerClient)
        {
            if(!blob.Name.Contains('/'))
            {
                throw new Exception($"Blob name expected to start with virtual folder named by checkpointId");
            }
            var client = containerClient.GetBlobClient(blob.Name);
            var blobDownloadStream = new MemoryStream();

            var response = await client.DownloadToAsync(blobDownloadStream);
            response.ThrowIfNotSuccessStatusCode();
            blobDownloadStream.Seek(0, SeekOrigin.Begin);

            return Tuple.Create(blob.Name.Split('/')[1], blobDownloadStream);
        }

        private void DeserializeToDictionary<T>(Tuple<string, MemoryStream> tuple, IDictionary<string, T> snapshots)
            where T : class
        {
            var (name, stream) = tuple;
            var downloadedObject = stream.BinaryDeserialize();
            var snapshot = downloadedObject as T ?? throw new Exception($"Downloaded blob did not contain expected {nameof(T)}");
            snapshots[name] = snapshot;
            stream.Dispose(); //ensure buffer memory is freed up
        }

        private Tuple<string, MemoryStream> SerializeSnapshotToStream(string snapshotKey, Checkpoint checkpoint)
        {
            var snapshot = checkpoint.GetSnapshot(snapshotKey);
            var snapshotUploadStream = new MemoryStream();
            snapshot.BinarySerializeTo(snapshotUploadStream);
            snapshotUploadStream.Seek(0, SeekOrigin.Begin);
            return Tuple.Create($"{checkpoint.Id}/{snapshotKey}", snapshotUploadStream);
        }

        private async Task UploadStreamToBlob(Tuple<string, MemoryStream> tuple, BlobContainerClient containerClient)
        {
            var (blobKey, stream) = tuple;
            var blobClient = containerClient.GetBlobClient(blobKey);
            await blobClient.UploadAsync(stream);
            stream.Dispose(); //ensure buffer memory is freed up
        }
        #endregion

        #region private metadata methods
        private async Task UploadCheckpointMetaData(Checkpoint checkpoint, BlobContainerClient blobContainerClient)
        {
            var blobMetaClient = blobContainerClient.GetBlobClient($"{checkpoint.Id}/meta");
            if (await blobMetaClient.ExistsAsync())
            {
                throw new Exception($"Attempted to upload metadata of checkpoint {checkpoint.Id}, which was already stored (duplicate id)");
            }

            var memStream = new MemoryStream();
            checkpoint.MetaData.BinarySerializeTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);
            await blobMetaClient.UploadAsync(memStream);
            memStream.Dispose();
        }

        private async Task<MetaData> DownloadCheckpointMetaData(Guid cpId, BlobContainerClient blobContainerClient)
        {
            var blobMetaClient = blobContainerClient.GetBlobClient($"{cpId}/meta");
            if (!await blobMetaClient.ExistsAsync())
            {
                throw new Exception($"Attempted to download metadata of checkpoint {cpId}, which was never stored (not found)");
            }

            var memStream = new MemoryStream();
            (await blobMetaClient.DownloadToAsync(memStream)).ThrowIfNotSuccessStatusCode();
            memStream.Seek(0, SeekOrigin.Begin);
            var dependencies = memStream.BinaryDeserialize() as MetaData;
            memStream.Dispose();

            return dependencies;
        }

        public async Task<IEnumerable<MetaData>> GetAllMetaData()
        {
            var blobContainerClient = GetBlobContainerClientForCheckpoints();
            //Define dataflow for metadata retrieval
            var metadatas = new ConcurrentBag<MetaData>();
            var downloadCheckpointMetaData = new ActionBlock<Guid>(
                async cpId => metadatas.Add(await DownloadCheckpointMetaData(cpId, blobContainerClient).ConfigureAwait(false)),
                _blockOptions
            );

            //Feed medatada blobs to dataflow
            var blobs = blobContainerClient.GetBlobs(BlobTraits.None, BlobStates.None); //async version of GetBlobs is not actually async.. so keeping it synchronous for now
            foreach (var blob in blobs.Where(bi => bi.Name.EndsWith("/meta")))
            {
                var idString = blob.Name.Split('/')[0];
                var checkpointId = Guid.Parse(idString);
                await downloadCheckpointMetaData.SendAsync(checkpointId).ConfigureAwait(false);
            }
            downloadCheckpointMetaData.Complete();
            //Wait for completion of deserialization
            await downloadCheckpointMetaData.Completion.ConfigureAwait(false);
            return metadatas;
        }
        #endregion
    }
}
