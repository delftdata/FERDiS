using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    [Serializable]
    public class Checkpoint
    {
        public Guid Id { get; }
        
        public MetaData MetaData { get; }
        
        /// <summary>
        /// Returns snapshot keys enumerable, usable for fetching snapshots
        /// </summary>
        public IEnumerable<string> Keys => _snapshots.Keys;

        private readonly IDictionary<string, ObjectSnapshot> _snapshots;
        
        public Checkpoint(Guid identifier, IDictionary<string, ObjectSnapshot> snapshots, MetaData metaData)
        {
            Id = identifier;
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
            MetaData = metaData ?? throw new ArgumentNullException(nameof(metaData));
        }

        public ObjectSnapshot GetSnapshot(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return _snapshots[key];
        }
    }
}
