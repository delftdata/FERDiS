using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Models
{
    [Serializable]
    [ProtoContract]
    public class MetaData
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }

        /// <summary>
        /// Dictionary containing the checkpoint id's of other (usually upstream) instances<br/>
        /// The checkpoint associated with this metadata should not be restored if a dependency is being restored.
        /// </summary>
        [ProtoMember(2)]
        public IDictionary<string, Guid> Dependencies { get; set; }

        [ProtoMember(3)]
        public string InstanceName { get; set; }

        [ProtoMember(4)]
        public DateTime CreatedAtUtc { get; set; }

        public MetaData(Guid id, IDictionary<string, Guid> dependencies, string instanceName, DateTime createdAtUtc)
        {
            Id = id;
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            InstanceName = instanceName ?? throw new ArgumentNullException(nameof(instanceName));
            if(createdAtUtc == default)
            {
                throw new ArgumentException("Invalid DateTime", nameof(createdAtUtc));
            }
            CreatedAtUtc = createdAtUtc;
        }

        public MetaData() { } //required for protobuf

        public class DependencyOrderComparer : IComparer<MetaData>
        {
            public int Compare(MetaData x, MetaData y)
            {
                var xDependsOnY = x.Dependencies[y.InstanceName] != default;
                var yDependsOnX = y.Dependencies[x.InstanceName] != default;

                if(xDependsOnY) { return -1; }
                else if(yDependsOnX) { return 1; }
                else { return 0; }
            }
        }
    }
}
