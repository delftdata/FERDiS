using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
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

namespace BlackSP.Core.Sources
{
    /// <summary>
    /// Control message source that watches the internal state based on various monitors. Creates new control messages based on state changes.
    /// </summary>
    public class WorkerStateChangeSource : ISource<ControlMessage>, IDisposable
    {
        private readonly WorkerStateMonitor _workerStateMonitor;
        private readonly ILogger _logger;

        /// <summary>
        /// local list of messages ready to be taken from this ISource<br/>
        /// Note how this implementation does not allow checkpointing due to the lack of synchronisation with the primary processing thread(s)
        /// </summary>
        private BlockingCollection<ControlMessage> _messages;

        private DateTime lastHeartBeat;
        private TimeSpan heartBeatInterval;
        private bool disposedValue;

        public WorkerStateChangeSource(WorkerStateMonitor workerStateMonitor, ILogger logger)
        {
            _workerStateMonitor = workerStateMonitor ?? throw new ArgumentNullException(nameof(workerStateMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _messages = new BlockingCollection<ControlMessage>(1 << 14);
            
            heartBeatInterval = TimeSpan.FromMilliseconds(1000 * 5);
            lastHeartBeat = DateTime.Now.Add(-heartBeatInterval);//make sure we start off with a heartbeat

            _workerStateMonitor.OnWorkersStart += WorkerStateMonitor_OnWorkersStart;//on subset ready to launch --> instruct launch
            _workerStateMonitor.OnWorkersHalt += WorkerStateMonitor_OnWorkersHalt;//on subset must halt --> instruct halt
            _workerStateMonitor.OnWorkersRestore += WorkerStateMonitor_OnWorkersRestore;//on subset must restore --> instruct restore
        }

        private void WorkerStateMonitor_OnWorkersRestore(IEnumerable<string> affectedInstanceNames)
        {
            if (!affectedInstanceNames.Any())
            {
                return;
            }

            //TODO: recovery line calculation
            Dictionary<string, Guid> checkpointMap = affectedInstanceNames.ToDictionary(name => name, name => Guid.NewGuid());

            var msg = new ControlMessage();
            msg.AddPayload(new CheckpointRestoreRequestPayload
            {
                InstanceCheckpointMap = checkpointMap
            });
            _messages.Add(msg);
        }

        private void WorkerStateMonitor_OnWorkersHalt(IEnumerable<string> affectedInstanceNames)
        {
            if (!affectedInstanceNames.Any())
            {
                return;
            }

            var msg = new ControlMessage();
            msg.AddPayload(new WorkerRequestPayload
            {
                RequestType = WorkerRequestType.StopProcessing,
                TargetInstanceNames = affectedInstanceNames
            });
            _messages.Add(msg);
        }

        private void WorkerStateMonitor_OnWorkersStart(IEnumerable<string> affectedInstanceNames)
        {
            if(!affectedInstanceNames.Any())
            {
                return;
            }

            var msg = new ControlMessage();
            msg.AddPayload(new WorkerRequestPayload
            {
                RequestType = WorkerRequestType.StartProcessing,
                TargetInstanceNames = affectedInstanceNames
            });
            _messages.Add(msg);
        }

        public Task Flush()
        {
            _messages.CompleteAdding();
            _messages = new BlockingCollection<ControlMessage>(1 << 14);
            return Task.CompletedTask; //nothing to flush here
        }

        public ControlMessage Take(CancellationToken t)
        {
            var timeSinceLastHeartbeat = DateTime.Now - lastHeartBeat;
            var timeTillNextHeartbeat = timeSinceLastHeartbeat > heartBeatInterval ? TimeSpan.Zero : heartBeatInterval - timeSinceLastHeartbeat;
            var timeoutSource = new CancellationTokenSource(timeTillNextHeartbeat);
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSource.Token);
            
            try
            {
                return _messages.Take(linkedSource.Token);
            }
            catch(OperationCanceledException)
            {
                _logger.Verbose($"No internal state changes for {heartBeatInterval.TotalSeconds} seconds, requesting heartbeat from workers");
                var msg = new ControlMessage(); //no new status-change message.. fall back to heartbeat request
                msg.AddPayload(new WorkerRequestPayload { RequestType = WorkerRequestType.Status });
                lastHeartBeat = DateTime.Now;
                return msg;
            }
            finally
            {
                timeoutSource.Dispose();
                linkedSource.Dispose();
            }
            
        }

        #region Dispose support
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _messages.CompleteAdding();
                    _messages.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WorkerStateChangeSource()
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
