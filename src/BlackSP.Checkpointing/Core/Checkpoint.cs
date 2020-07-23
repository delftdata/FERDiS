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

        /// <summary>
        /// Returns snapshot keys enumerable, usable for fetching snapshots
        /// </summary>
        public IEnumerable<string> Keys => _snapshots.Keys;
        
        private readonly Guid _identifier;
        private readonly IDictionary<string, ObjectSnapshot> _snapshots;
        private readonly IDictionary<string, Guid> _dependencies;

        
        public Checkpoint(Guid identifier, IDictionary<string, ObjectSnapshot> snapshots, IDictionary<string, Guid> dependencies)
        {
            _identifier = identifier;
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public ObjectSnapshot GetSnapshot(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return _snapshots[key];
        }

        /// <summary>
        /// returns dictionary with key: Vertex instance name and value: checkpointId of that instance
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, Guid> GetDependencies()
        {
            return _dependencies;
        }


    }
}
