using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageSources
{
    /// <summary>
    /// Control message source that watches the internal state based on various monitors. Creates new control messages based on state changes.
    /// </summary>
    public class InternalStateChangeSource : IMessageSource<ControlMessage>
    {

        private readonly ConnectionMonitor _connectionMonitor;
        private readonly WorkerStateMonitor _workerStateMonitor;

        /// <summary>
        /// 
        /// </summary>
        private BlockingCollection<ControlMessage> _messages;

        /// <summary>
        /// Defaults to true during startup, once all workers connected turns false and stays that way.
        /// </summary>
        private bool initializing;

        public InternalStateChangeSource(ConnectionMonitor connectionMonitor, WorkerStateMonitor workerStateMonitor)
        {
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _workerStateMonitor = workerStateMonitor ?? throw new ArgumentNullException(nameof(workerStateMonitor));

            _messages = new BlockingCollection<ControlMessage>();
            initializing = true;
            _connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChangeEvent;
        }

        private void ConnectionMonitor_OnConnectionChangeEvent(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            if(e.UpstreamFullyConnected && e.DownstreamFullyConnected)
            {
                //all workers are connected
                Console.WriteLine("Coordinator is fully connected.");
                var msg = new ControlMessage();
                msg.AddPayload(new WorkerRequestPayload { RequestType = WorkerRequestType.StartProcessing });
                _messages.Add(msg);
                initializing = false;
            } 
            else if(initializing)
            {
                Console.WriteLine("Coordinator not yet fully connected.");
            } 
            else
            {
                Console.WriteLine("Coordinator detected a worker failure.");
                //TODO: get who failed?
            }
        }

        public Task Flush()
        {
            _messages.CompleteAdding();
            _messages = new BlockingCollection<ControlMessage>();
            return Task.CompletedTask; //nothing to flush here
        }

        public ControlMessage Take(CancellationToken t)
        {
            var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, timeoutSource.Token);
            
            try
            {
                return _messages.Take(linkedSource.Token);
            }
            catch(OperationCanceledException)
            {
                Console.WriteLine("No internal state changes, requesting heartbeats");
                var msg = new ControlMessage(); //no new status-change message.. fall back to heartbeat request
                msg.AddPayload(new WorkerRequestPayload { RequestType = WorkerRequestType.Status });
                return msg;
            }
            finally
            {
                timeoutSource.Dispose();
                linkedSource.Dispose();
            }
            
        }
    }
}
