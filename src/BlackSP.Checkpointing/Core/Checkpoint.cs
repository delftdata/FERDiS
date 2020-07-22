using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    [Serializable]
    public class Checkpoint
    {
        public Guid Id => _identifier;
        public IEnumerable<string> Keys => _snapshots.Keys;
        
        private readonly Guid _identifier;
        private readonly IDictionary<string, ObjectSnapshot> _snapshots;

        public Checkpoint(Guid identifier, IDictionary<string, ObjectSnapshot> snapshots)
        {
            _identifier = identifier;
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        }

        public ObjectSnapshot GetSnapshot(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return _snapshots[key];
        }
    }
}
