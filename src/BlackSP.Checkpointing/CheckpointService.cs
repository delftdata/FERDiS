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
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Logging;
using System.Diagnostics;

namespace BlackSP.Checkpointing
{
    ///<inheritdoc/>
    public class CheckpointService : ICheckpointService
    {

        public event BeforeCheckpointEvent BeforeCheckpointTaken;
        public event AfterCheckpointEvent AfterCheckpointTaken;

        private readonly ObjectRegistry _register;
        private readonly CheckpointDependencyTracker _dpTracker;
        private readonly RecoveryLineCalculator.Factory _rlCalcFactory;
        private readonly ICheckpointStorage _storage;
        private readonly ICheckpointConfiguration _checkpointConfiguration;
        private readonly IMetricLogger _logger;

        public CheckpointService(ObjectRegistry register, 
                                 CheckpointDependencyTracker dependencyTracker,
                                 RecoveryLineCalculator.Factory rlCalcFactory,
                                 ICheckpointStorage checkpointStorage, 
                                 ICheckpointConfiguration checkpointConfiguration,
                                 IMetricLogger logger)
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
            _logger.GetDefaultLogger().Debug($"Updating checkpoint dependency on instance {originInstanceName} to {checkpointId}");
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

        public async Task<int> CollectGarbageAfterRecoveryLine(IRecoveryLine recoveryLine)
        {
            _ = recoveryLine ?? throw new ArgumentNullException(nameof(recoveryLine));
            var allCheckpointMetas = await _storage.GetAllMetaData().ConfigureAwait(false);
            var cpStacksPerInstance = allCheckpointMetas
                .GroupBy(m => m.InstanceName)
                .Select(group => group.OrderBy(m => m.CreatedAtUtc))
                .Select(sortedGroup => new Stack<MetaData>(sortedGroup))
                .ToList();
            var garbage = new List<MetaData>();
            foreach(var stack in cpStacksPerInstance)
            {
                var instanceName = stack.Peek().InstanceName;
                if(!recoveryLine.AffectedWorkers.Contains(instanceName))
                {
                    continue;
                }
                var cpId = recoveryLine.RecoveryMap[instanceName];
                while(stack.Peek().Id != cpId)
                {
                    garbage.Add(stack.Pop());
                }
            }
            await Task.WhenAll(garbage.Select(meta => _storage.Delete(meta.Id))).ConfigureAwait(false);
            return garbage.Count;
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
            if (o.AssertCheckpointability())
            {
                _logger.GetDefaultLogger().Debug($"Object of type {type.Name} is registered for checkpointing with {nameof(CheckpointService)}.");
                _register.Add(identifier, o);
                return true;
            }
            return false;
        }

        ///<inheritdoc/>
        public async Task<Guid> TakeCheckpoint(string currentInstanceName, bool isForced = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            //- actual behavior
            BeforeCheckpointTaken?.Invoke();
            var snapshots = _register.TakeObjectSnapshots();
            var metadata = new MetaData(Guid.NewGuid(), _dpTracker.Dependencies, currentInstanceName, DateTime.UtcNow);
            var checkpoint = new Checkpoint(metadata, snapshots);
            var size = await _storage.Store(checkpoint).ConfigureAwait(false);
            _dpTracker.UpdateDependency(currentInstanceName, checkpoint.Id); //ensure next checkpoint depends on current
            AfterCheckpointTaken?.Invoke(metadata.Id);
            //- end of actual behavior
            sw.Stop();
            _logger.Checkpoint(size, sw.Elapsed, isForced);
            return checkpoint.Id;
        }

        public async Task ClearCheckpointStorage()
        {
            _logger.GetDefaultLogger().Debug("Will clear checkpoint storage");
            var allCheckpointMetadatas = await _storage.GetAllMetaData().ConfigureAwait(false);
            var deleteTasks = allCheckpointMetadatas.Select(meta => _storage.Delete(meta.Id));
            await Task.WhenAll(deleteTasks).ConfigureAwait(false);
            _logger.GetDefaultLogger().Debug("Checkpoint storage cleared successfully");
        }

        ///<inheritdoc/>
        public async Task RestoreCheckpoint(Guid checkpointId)
        {
            var sw = new Stopwatch();
            sw.Start();

            var checkpoint = (await _storage.Retrieve(checkpointId).ConfigureAwait(false)) 
                ?? throw new CheckpointRestorationException($"Checkpoint storage returned null for checkpoint ID: {checkpointId}");
            _register.RestoreCheckpoint(checkpoint);
            _dpTracker.OverwriteDependencies(checkpoint.MetaData.Dependencies);
            _dpTracker.UpdateDependency(checkpoint.MetaData.InstanceName, checkpoint.Id); //ensure next checkpoint depends on current

            sw.Stop();
            var milisecondsReverted = DateTime.UtcNow - checkpoint.MetaData.CreatedAtUtc;
            _logger.Recovery(sw.Elapsed, milisecondsReverted);
        }

    }
}
