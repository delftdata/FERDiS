using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private DateTime lastHeartBeat;
        private TimeSpan heartBeatInterval;

        public InternalStateChangeSource(ConnectionMonitor connectionMonitor, WorkerStateMonitor workerStateMonitor)
        {
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _workerStateMonitor = workerStateMonitor ?? throw new ArgumentNullException(nameof(workerStateMonitor));

            _messages = new BlockingCollection<ControlMessage>();
            
            initializing = true;
            heartBeatInterval = TimeSpan.FromSeconds(5);
            lastHeartBeat = DateTime.Now.Add(-heartBeatInterval);
            //_connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChangeEvent;

            
            //on subset ready to rollback --> instruct rollback
            //on failure --> instruct halt downstream
            _workerStateMonitor.OnWorkersStart += WorkerStateMonitor_OnWorkersStart;//on subset ready to launch --> instruct launch
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

        private void ConnectionMonitor_OnConnectionChangeEvent(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            if(e.UpstreamFullyConnected && e.DownstreamFullyConnected)
            {
                //all workers are connected
                Console.WriteLine("Coordinator is fully connected.");
            } 
            else if(initializing)
            {
                Console.WriteLine("Coordinator not yet fully connected.");
            } 
            else
            {
                Console.WriteLine("Coordinator detected a worker failure.");
                //TODO: get who failed?
                e.ChangedConnection.Item1.Endpoint.RemoteInstanceNames.ElementAt(e.ChangedConnection.Item1.ShardId);
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
                //Console.WriteLine("No internal state changes, requesting heartbeat from workers");
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
    }
}
