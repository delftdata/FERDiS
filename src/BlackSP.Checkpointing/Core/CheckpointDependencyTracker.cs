using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Core
{

    public class CheckpointDependencyTracker
    {
        /// <summary>
        /// Public dependency dict, note the internal dictionary is not modifyable through this public one
        /// </summary>
        public IDictionary<string, Guid> Dependencies => new Dictionary<string, Guid>(dependencies);
        
        private IDictionary<string, Guid> dependencies;

        public CheckpointDependencyTracker() 
        {
            dependencies = new Dictionary<string, Guid>();
        }

        /// <summary>
        /// Adds a new checkpoint dependency with respect to an origin instance name.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="checkpointId"></param>
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
