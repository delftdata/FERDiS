using BlackSP.Checkpointing.Core;
using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.Persistence
{
    abstract class CheckpointStorageBase : ICheckpointStorage
    {

        private readonly RecyclableMemoryStreamManager _streamManager;

        public CheckpointStorageBase(RecyclableMemoryStreamManager streamManager)
        {
            _streamManager = streamManager ?? throw new ArgumentNullException(nameof(streamManager));
        }

        public Task<Checkpoint> Retrieve(Guid id)
        {
            var serializedSnapshots = new BlockingCollection<Stream>(1);

            throw new NotImplementedException();
        }

        public Task Store(Checkpoint checkpoint)
        {
            throw new NotImplementedException();
        }
    }
}
