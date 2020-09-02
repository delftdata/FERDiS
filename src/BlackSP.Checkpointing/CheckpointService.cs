﻿using BlackSP.Checkpointing.Extensions;
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

namespace BlackSP.Checkpointing
{
    ///<inheritdoc/>
    public class CheckpointService : ICheckpointService
    {

        private readonly ObjectRegistry _register;
        private readonly CheckpointDependencyTracker _dpTracker;
        private readonly ICheckpointStorage _storage;
        private readonly ILogger _logger;

        public CheckpointService(ObjectRegistry register, 
                                 CheckpointDependencyTracker dependencyTracker,
                                 
                                 ICheckpointStorage checkpointStorage, 
                                 ILogger logger)
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

        public async Task<IRecoveryLine> CalculateRecoveryLine()
        {
            IEnumerable<string> failedInstances = Enumerable.Empty<string>();//todo make argument (also interface)

            var allCheckpointMetadatas = await _storage.GetAllMetaData().ConfigureAwait(false);
            var cpStacksPerInstance = allCheckpointMetadatas
                .GroupBy(m => m.InstanceName)
                .Select(group => group.OrderBy(m => m.CreatedAtUtc))
                .Select(sortedGroup => new Stack<MetaData>(sortedGroup));

            //TODO: push future checkpoint on any non-failed instance's stack
            foreach(var cpStack in cpStacksPerInstance)
            {
                if (failedInstances.Contains(cpStack.Peek().InstanceName))
                {
                    continue; //skip failed instances
                }
                //create future checkpoint meta data (default id?)
                //- need to know direct upstream 
                //push future checkpoint 
            }
            //TODO: pop off dependencies that depend on other stack tops

            IDictionary<string, Guid> recoveryMap = cpStacksPerInstance.Select(metaStack => metaStack.Peek()) //select the remaining checkpoint atop each stack
                .ToDictionary(meta => meta.InstanceName, meta => meta.Id);

            return new RecoveryLine(recoveryMap);
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
        public async Task<Guid> TakeCheckpoint(string currentInstanceName)
        {
            var snapshots = _register.TakeObjectSnapshots();
            var metadata = new MetaData(Guid.NewGuid(), _dpTracker.Dependencies, currentInstanceName, DateTime.UtcNow);
            var checkpoint = new Checkpoint(metadata, snapshots);
            await _storage.Store(checkpoint).ConfigureAwait(false);
            _dpTracker.UpdateDependency(currentInstanceName, checkpoint.Id); //ensure next checkpoint depends on current
            return checkpoint.Id;
        }

        ///<inheritdoc/>
        public async Task RestoreCheckpoint(Guid checkpointId)
        {
            var checkpoint = (await _storage.Retrieve(checkpointId)) 
                ?? throw new CheckpointRestorationException($"Checkpoint storage returned null for checkpoint ID: {checkpointId}");
            _register.RestoreCheckpoint(checkpoint);
            _dpTracker.OverwriteDependencies(checkpoint.MetaData.Dependencies);
        }

    }
}
