using BlackSP.Checkpointing.Extensions;
using BlackSP.Checkpointing.Core;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Serialization.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Checkpointing.Exceptions;
using System.Threading.Tasks;
using Serilog;
using System.Collections.Immutable;
using BlackSP.Kernel.Models;
using BlackSP.Checkpointing.Models;

namespace BlackSP.Checkpointing
{
    ///<inheritdoc/>
    public class CheckpointService : ICheckpointService
    {

        private readonly ObjectRegistry _register;
        private readonly CheckpointDependencyTracker _dpTracker;
        private readonly RecoveryLineCalculator.Factory _rlCalcFactory;
        private readonly ICheckpointStorage _storage;
        private readonly ICheckpointConfiguration _checkpointConfiguration;
        private readonly ILogger _logger;

        public CheckpointService(ObjectRegistry register, 
                                 CheckpointDependencyTracker dependencyTracker,
                                 RecoveryLineCalculator.Factory rlCalcFactory,
                                 ICheckpointStorage checkpointStorage, 
                                 ICheckpointConfiguration checkpointConfiguration,
                                 ILogger logger)
        {
            _register = register ?? throw new ArgumentNullException(nameof(register));
            _dpTracker = dependencyTracker ?? throw new ArgumentNullException(nameof(dependencyTracker));
            _storage = checkpointStorage ?? throw new ArgumentNullException(nameof(checkpointStorage));
            _rlCalcFactory = rlCalcFactory ?? throw new ArgumentNullException(nameof(rlCalcFactory));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        ///<inheritdoc/>
        public void UpdateCheckpointDependency(string originInstanceName, Guid checkpointId)
        {
            _dpTracker.UpdateDependency(originInstanceName, checkpointId);
        }

        public Guid GetLastCheckpointId(string currentInstanceName)
        {
            try
            {
                return _dpTracker.Dependencies[currentInstanceName];
            } 
            catch(KeyNotFoundException)
            {
                return Guid.Empty;
            }
        }

        public Guid GetSecondLastCheckpointId(string currentInstanceName)
        {
            try
            {
                return _dpTracker.GetPreviousDependency(currentInstanceName);
            }
            catch (KeyNotFoundException)
            {
                return Guid.Empty;
            }
        }

        ///<inheritdoc/>
        public async Task<IRecoveryLine> CalculateRecoveryLine(IEnumerable<string> failedInstanceNames)
        {
            var allCheckpointMetadatas = await _storage.GetAllMetaData().ConfigureAwait(false);
            var calculator = _rlCalcFactory.Invoke(allCheckpointMetadatas);
            return calculator.CalculateRecoveryLine(_checkpointConfiguration.AllowReusingState, failedInstanceNames);
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
                throw new NotSupportedException($"Registering multiple instances of the same type is not supported - type: {type.Name}");
            }
            
            if(!o.AssertCheckpointability())
            {
                return false;
            }
            _logger.Verbose($"Object of type {type.Name} is registered for checkpointing with {nameof(CheckpointService)}.");
            _register.Add(identifier, o);
            return true;
        }

        ///<inheritdoc/>
        public async Task<Guid> TakeCheckpoint(string currentInstanceName)
        {
            var snapshots = _register.TakeObjectSnapshots();
            var metadata = new MetaData(Guid.NewGuid(), _dpTracker.Dependencies, currentInstanceName, DateTime.UtcNow);
            var checkpoint = new Checkpoint(metadata, snapshots);
            await _storage.Store(checkpoint).ConfigureAwait(false);
            _dpTracker.UpdateDependency(currentInstanceName, checkpoint.Id); //ensure next checkpoint depends on current
            return checkpoint.Id;
        }

        public async Task ClearCheckpointStorage()
        {
            _logger.Debug("Starting checkpoint storage clear");
            var allCheckpointMetadatas = await _storage.GetAllMetaData().ConfigureAwait(false);
            var deleteTasks = allCheckpointMetadatas.Select(meta => _storage.Delete(meta.Id));
            await Task.WhenAll(deleteTasks).ConfigureAwait(false);
            _logger.Debug("Checkpoint storage cleared successfully");
        }

        ///<inheritdoc/>
        public async Task RestoreCheckpoint(Guid checkpointId)
        {
            var checkpoint = (await _storage.Retrieve(checkpointId).ConfigureAwait(false)) 
                ?? throw new CheckpointRestorationException($"Checkpoint storage returned null for checkpoint ID: {checkpointId}");
            _register.RestoreCheckpoint(checkpoint);
            _dpTracker.OverwriteDependencies(checkpoint.MetaData.Dependencies);
            _dpTracker.UpdateDependency(checkpoint.MetaData.InstanceName, checkpoint.Id); //ensure next checkpoint depends on current
        }

    }
}
