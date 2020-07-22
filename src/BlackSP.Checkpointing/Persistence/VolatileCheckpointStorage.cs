using BlackSP.Checkpointing.Core;
using BlackSP.Serialization.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.Persistence
{
    public class VolatileCheckpointStorage : ICheckpointStorage
    {

        private Dictionary<Guid, byte[]> _store;
        private BinaryFormatter _formatter;
        public VolatileCheckpointStorage()
        {
            _store = new Dictionary<Guid, byte[]>();
            _formatter = new BinaryFormatter();
        }

        public Task<Checkpoint> Retrieve(Guid id)
        {
            var blob = _store[id];
            return Task.FromResult((Checkpoint)blob.BinaryDeserialize());
        }

        public Task Store(Checkpoint checkpoint)
        {
            var blob = checkpoint.BinarySerialize();
            _store.Add(checkpoint.Id, blob);
            return Task.CompletedTask;
        }
    }
}
