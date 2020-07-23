using BlackSP.Checkpointing.Extensions;
using BlackSP.Checkpointing.Core;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Serialization.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Checkpointing.Exceptions;
using System.Threading.Tasks;
using Serilog;

namespace BlackSP.Checkpointing
{
    ///<inheritdoc/>
    public class CheckpointService : ICheckpointService
    {

        private readonly ObjectRegistry _register;
        private readonly CheckpointDependencyTracker _dpTracker;
        private readonly ICheckpointStorage _storage;
        private readonly ILogger _logger;

        public CheckpointService(ObjectRegistry register, CheckpointDependencyTracker dependencyTracker, ICheckpointStorage checkpointStorage, ILogger logger)
        {
            _register = register ?? throw new ArgumentNullException(nameof(register));
            _storage = checkpointStorage ?? throw new ArgumentNullException(nameof(checkpointStorage));
            _dpTracker = dependencyTracker ?? throw new ArgumentNullException(nameof(dependencyTracker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void UpdateCheckpointDependency(string origin, Guid checkpointId)
        {
            _dpTracker.UpdateDependency(origin, checkpointId);
        }

        ///<inheritdoc/>
        public bool RegisterObject(object o)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));

            var type = o.GetType();
            var identifier = type.AssemblyQualifiedName; //Note: currently the implementation supports only one instance of any concrete type 
            //Under the assumption of exact same object registration order (at least regarding instances of the same type) this could be extended to support multiple instances
            if(_register.Contains(identifier))
            {
                throw new NotSupportedException($"Registering multiple instances of the same type is not supported");
            }
            
            if(!o.AssertCheckpointability())
            {
                return false;
            }
            _logger.Verbose($"Object of type {type} is registered for checkpointing with {nameof(CheckpointService)}.");
            _register.Add(identifier, o);
            return true;
        }

        ///<inheritdoc/>
        public async Task<Guid> TakeCheckpoint()
        {
            var checkpoint = _register.TakeCheckpoint(_dpTracker);
            await _storage.Store(checkpoint);
            return checkpoint.Id;
        }

        ///<inheritdoc/>
        public async Task RestoreCheckpoint(Guid checkpointId)
        {
            var checkpoint = (await _storage.Retrieve(checkpointId)) 
                ?? throw new CheckpointRestorationException($"Checkpoint storage returned null for checkpoint ID: {checkpointId}");
            _register.RestoreCheckpoint(checkpoint);
            _dpTracker.OverwriteDependencies(checkpoint.GetDependencies());
        }

    }
}
