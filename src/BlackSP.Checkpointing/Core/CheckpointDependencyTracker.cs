using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    public class CheckpointDependencyTracker
    {
        public IDictionary<string, Guid> Dependencies => dependencies;
        private IDictionary<string, Guid> dependencies;

        public CheckpointDependencyTracker() 
        {
            dependencies = new Dictionary<string, Guid>();
        }

        public void UpdateDependency(string origin, Guid checkpointId)
        {
            dependencies[origin] = checkpointId;
        }

        public void OverwriteDependencies(IDictionary<string, Guid> newDependencies)
        {
            dependencies = newDependencies ?? throw new ArgumentNullException(nameof(newDependencies));
        }
    }
}
