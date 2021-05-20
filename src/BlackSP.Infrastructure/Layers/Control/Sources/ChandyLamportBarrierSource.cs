using BlackSP.Checkpointing;
using BlackSP.Core;
using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Sources
{
    /// <summary>
    /// Message source that targets source operators to kick off the chandy-lamport barrier-based algorithm
    /// </summary>
    public class ChandyLamportBarrierSource : ISource<ControlMessage>, IDisposable
    {
        public (IEndpointConfiguration, int) MessageOrigin => (null, 0);

        private readonly WorkerGraphStateManager _graphStateManager;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ICheckpointConfiguration _checkpointConfiguration;
        private readonly ILogger _logger;

        private DateTime lastCheckpointUtc;
        private readonly TimeSpan _globalCheckpointInterval;
        private readonly Timer _globalCheckpointTimer;
        private readonly Channel<ControlMessage> _messages;

        private bool _timerActive;
        
        private bool disposedValue;

        //TODO: consider building in a feedback loop to detect completion of global checkpoint

        public ChandyLamportBarrierSource(WorkerGraphStateManager graphStateManager,
            IVertexGraphConfiguration graphConfiguration,
            IVertexConfiguration vertexConfiguration,
            ICheckpointConfiguration checkpointConfiguration,
            ILogger logger)
        {
            _graphStateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _globalCheckpointInterval = TimeSpan.FromSeconds(_checkpointConfiguration.CheckpointIntervalSeconds);
            _messages = Channel.CreateUnbounded<ControlMessage>();
            _globalCheckpointTimer = new Timer(CreateBarrierMessagesForSources, null, int.MaxValue, int.MaxValue);
            _timerActive = false;

            foreach (var manager in _graphStateManager.GetAllWorkerStateManagers())
            {
                manager.OnStateChange += WorkerStateManager_OnStateChange;
            }
        }

        public Task Flush(IEnumerable<string> upstreamInstancesToFlush)
        {
            return Task.CompletedTask;
        }

        public async Task<ControlMessage> Take(CancellationToken t)
        {
            return await _messages.Reader.ReadAsync(t);
        }

        private void CreateBarrierMessagesForSources(object _)
        {
            //data connections = all connections that do not include connections to the current vertex (assumed to be coordinator)
            var dataConnections = _graphConfiguration.InstanceConnections.Where(pair => pair.Item1 != _vertexConfiguration.InstanceName && pair.Item2 != _vertexConfiguration.InstanceName);
            //from the data connections pick the instances without incoming connections, those must be the sources
            var sourceInstances = _graphConfiguration.InstanceNames.Where(name => !dataConnections.Any(pair => pair.Item2 == name));
            foreach(var instanceName in sourceInstances)
            {
                var msg = new ControlMessage(_vertexConfiguration.GetPartitionKeyForInstanceName(instanceName));
                msg.AddPayload(new BarrierPayload());
                while(!_messages.Writer.TryWrite(msg)){}
                _logger.Information($"Generated barrier for source {instanceName}");
            }
            CheckpointTimer(false);
            lastCheckpointUtc = DateTime.UtcNow;
        }

        private void WorkerStateManager_OnStateChange(string affectedInstanceName, WorkerState newState)
        {
            if (_timerActive && !_graphStateManager.AreAllWorkersInState(WorkerState.Running))
            {   //not all workers are running, temporarily suspend global checkpoint timer
                CheckpointTimer(false);
            }
            else if(!_timerActive && _graphStateManager.AreAllWorkersInState(WorkerState.Running))
            {
                if(lastCheckpointUtc == default)
                {
                    lastCheckpointUtc = DateTime.UtcNow;
                }
                CheckpointTimer(true);
            }
        }

        public void CheckpointTimer(bool enable)
        {
            if(!enable)
            {
                _logger.Information("Suspending global checkpoint timer");
                _globalCheckpointTimer.Change(int.MaxValue, int.MaxValue);
                _timerActive = false;
            } 
            else
            {
                var timeSinceLastCp = DateTime.UtcNow - lastCheckpointUtc;
                var nextCPDue = timeSinceLastCp < _globalCheckpointInterval ? _globalCheckpointInterval - timeSinceLastCp : _globalCheckpointInterval;
                _logger.Information($"Resuming global checkpoint timer, due in {nextCPDue.TotalSeconds} seconds");
                _globalCheckpointTimer.Change(nextCPDue, _globalCheckpointInterval);
                _timerActive = true;
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _globalCheckpointTimer?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        
    }
}
