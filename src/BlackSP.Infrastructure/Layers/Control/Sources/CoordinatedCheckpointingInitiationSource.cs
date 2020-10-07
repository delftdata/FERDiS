using BlackSP.Core;
using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Infrastructure.Layers.Data.Payloads;
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
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Sources
{
    /// <summary>
    /// Message source that targets source operators to kick off the chandy-lamport barrier-based algorithm
    /// </summary>
    public class CoordinatedCheckpointingInitiationSource : ISource<ControlMessage>, IDisposable
    {
        public (IEndpointConfiguration, int) MessageOrigin => (null, 0);

        private readonly WorkerGraphStateManager _graphStateManager;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;
        private readonly TimeSpan _globalCheckpointInterval;
        private readonly Timer _globalCheckpointTimer;
        private readonly BlockingCollection<ControlMessage> _messages;

        private bool _timerActive;
        
        private bool disposedValue;

        public CoordinatedCheckpointingInitiationSource(WorkerGraphStateManager graphStateManager,
            IVertexGraphConfiguration graphConfiguration,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _graphStateManager = graphStateManager ?? throw new ArgumentNullException(nameof(graphStateManager));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _globalCheckpointInterval = TimeSpan.FromMinutes(10); //TODO: make configurable?
            _messages = new BlockingCollection<ControlMessage>();
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

        public Task<ControlMessage> Take(CancellationToken t)
        {
            return Task.FromResult(_messages.Take(t));
        }

        private void CreateBarrierMessagesForSources(object _)
        {
            //data connections = all connections that do not include connections to the current vertex (assumed to be coordinator)
            var dataConnections = _graphConfiguration.InstanceConnections.Where(pair => pair.Item1 != _vertexConfiguration.InstanceName && pair.Item2 != _vertexConfiguration.InstanceName);
            //from the data connections pick the instances without incoming connections, those must be the sources
            var sourceInstances = _graphConfiguration.InstanceNames.Where(name => !dataConnections.Any(pair => pair.Item2 == name));
            foreach(var instanceName in sourceInstances)
            {
                _logger.Debug($"Generated barrier message for {instanceName}");
                var msg = new ControlMessage(_vertexConfiguration.GetPartitionKeyForInstanceName(instanceName));
                msg.AddPayload(new BarrierPayload());
                _messages.Add(msg);
            }
        }

        private void WorkerStateManager_OnStateChange(string affectedInstanceName, WorkerState newState)
        {
            if (_timerActive && !_graphStateManager.AreAllWorkersInState(WorkerState.Running))
            {   //not all workers are running, temporarily suspend global checkpoint timer
                _logger.Information("Suspending global checkpoint timer, not all workers are operational");
                _globalCheckpointTimer.Change(int.MaxValue, int.MaxValue);
                _timerActive = false;
            }
            else if(!_timerActive && _graphStateManager.AreAllWorkersInState(WorkerState.Running))
            {
                _logger.Information("Resuming global checkpoint timer, all workers are operational");
                _globalCheckpointTimer.Change(_globalCheckpointInterval, _globalCheckpointInterval);
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
                    _messages?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CoordinatedCheckpointingInitiationSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        
    }
}
