using BlackSP.Core;
using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Sources
{
    /// <summary>
    /// Generates worker request messages based on internal state changes
    /// </summary>
    public class WorkerRequestSource : ISource<ControlMessage>, IDisposable
    {

        public (IEndpointConfiguration, int) MessageOrigin => (null, 0); //origin is local so no information to share


        private readonly WorkerGraphStateManager _graphManager;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly ILogger _logger;

        /// <summary>
        /// local list of messages ready to be taken from this ISource<br/>
        /// Note how this implementation does not allow checkpointing due to the lack of synchronisation with the primary processing thread(s)
        /// </summary>
        private BlockingCollection<ControlMessage> messages;
        private DateTime lastHeartBeat;
        private TimeSpan heartbeatInterval;
        private bool disposedValue;

        public WorkerRequestSource(WorkerGraphStateManager graphManager, 
            ConnectionMonitor connectionMonitor, 
            IVertexConfiguration vertexConfiguration, 
            IVertexGraphConfiguration graphConfiguration,
            ILogger logger)
        {
            _graphManager = graphManager ?? throw new ArgumentNullException(nameof(graphManager));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            messages = new BlockingCollection<ControlMessage>(Constants.DefaultThreadBoundaryQueueSize);
            heartbeatInterval = TimeSpan.FromSeconds(Constants.HeartbeatSeconds);
            lastHeartBeat = DateTime.Now.Add(-heartbeatInterval);//make sure we start off with a heartbeat

            RegisterWorkerStateChangeEventHandlers();
            _graphManager.ListenTo(connectionMonitor);
        }

        public Task<ControlMessage> Take(CancellationToken t)
        {
            var timeSinceLastHeartbeat = DateTime.Now - lastHeartBeat;
            var timeTillNextHeartbeat = timeSinceLastHeartbeat > heartbeatInterval ? TimeSpan.Zero : heartbeatInterval - timeSinceLastHeartbeat;
            var timeoutSource = new CancellationTokenSource(timeTillNextHeartbeat);
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSource.Token);

            ControlMessage message;
            try
            {
                message = messages.Take(linkedSource.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.Verbose($"No internal state changes for {heartbeatInterval.TotalSeconds} seconds, preparing status request message for all workers");
                var msg = new ControlMessage(); //no new status-change message.. fall back to heartbeat request (note: no partitionkey as we want to broadcast this)
                msg.AddPayload(new WorkerRequestPayload { RequestType = WorkerRequestType.Status });
                lastHeartBeat = DateTime.Now;
                message = msg;
            }
            finally
            {
                timeoutSource.Dispose();
                linkedSource.Dispose();
            }
            return Task.FromResult(message);
        }

        public Task Flush(IEnumerable<string> upstreamInstancesToFlush)
        {
            messages.CompleteAdding();
            messages = new BlockingCollection<ControlMessage>(Constants.DefaultThreadBoundaryQueueSize);
            return Task.CompletedTask; //nothing to flush here
        }

        private void RegisterWorkerStateChangeEventHandlers()
        {
            foreach(var manager in _graphManager.GetAllWorkerStateManagers())
            {
                manager.OnStateChangeNotificationRequired += WorkerStateManager_OnStateChangeNotificationRequired;
            }
        }

        private void WorkerStateManager_OnStateChangeNotificationRequired(string affectedInstanceName, WorkerState newState)
        {
            int partitionKey = _vertexConfiguration.GetPartitionKeyForInstanceName(affectedInstanceName);
            var msg = new ControlMessage(partitionKey);
            switch (newState)
            {
                case WorkerState.Running:
                    msg.AddPayload(new WorkerRequestPayload { RequestType = WorkerRequestType.StartProcessing });
                    break;
                case WorkerState.Halting:
                    msg.AddPayload(GetWorkerRequestPayloadForHaltingInstance(affectedInstanceName));
                    break;
                case WorkerState.Recovering:
                    var targetCheckpointId = _graphManager.GetWorkerStateManager(affectedInstanceName).RestoringCheckpointId;
                    msg.AddPayload(new CheckpointRestoreRequestPayload(targetCheckpointId));
                    break;
                default:
                    throw new InvalidOperationException($"Attempted to determine notification message for instance {affectedInstanceName} with WorkerState {newState}, which is not implemented in {this.GetType()}");
            }
            messages.Add(msg);
        }

        private WorkerRequestPayload GetWorkerRequestPayloadForHaltingInstance(string instanceName)
        {
            var workerManager = _graphManager.GetWorkerStateManager(instanceName);
            if(workerManager.DataProcessorHaltArgs == default)
            {
                throw new InvalidOperationException($"Attempting to halt worker {instanceName} without supplying required arguments");
            }
            
            var (downstreamWorkersThatAreHalting, upstreamWorkersThatAreHalting) = workerManager.DataProcessorHaltArgs;
            return new WorkerRequestPayload
            {
                RequestType = WorkerRequestType.StopProcessing,
                UpstreamHaltingInstances = upstreamWorkersThatAreHalting,
                DownstreamHaltingInstances = downstreamWorkersThatAreHalting
            };
        }

        #region dispose pattern

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                messages.Dispose();
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WorkerRequestSource()
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
        #endregion
    }
}
