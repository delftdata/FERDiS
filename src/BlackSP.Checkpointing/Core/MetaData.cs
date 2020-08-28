using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    [Serializable]
    public class MetaData
    {
        public IDictionary<string, Guid> Dependencies { get; private set; }

        public string InstanceName { get; private set; }

        public MetaData(IDictionary<string, Guid> dependencies, string instanceName)
        {
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            InstanceName = instanceName ?? throw new ArgumentNullException(nameof(instanceName));
        }
    }
}
