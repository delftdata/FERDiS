using BlackSP.Core.Extensions;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Monitors
{
    public enum WorkerState
    {
        Offline,
        Launchable,
        Launched,
        Halted,
        Restoring,
        Faulted
    }

    /// <summary>
    /// Tracks perceived state of all workers in the vertex graph.
    /// </summary>
    public class WorkerStateMonitor
    {

        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly Dictionary<string, WorkerState> _workerStates;

        public delegate void AffectedWorkersEventHandler(IEnumerable<string> affectedInstanceNames);
        public event AffectedWorkersEventHandler OnWorkersStart;
        public event AffectedWorkersEventHandler OnWorkersRestore;
        public event AffectedWorkersEventHandler OnWorkersHalt;

        public WorkerStateMonitor(IVertexGraphConfiguration graphConfiguration, IVertexConfiguration vertexConfiguration, ConnectionMonitor connectionMonitor)
        {
            //assumption: current vertexconfiguration is that of coordinator --> only has control channels to all vertices in system
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChange;
            
            _workerStates = new Dictionary<string, WorkerState>();
            InitializeInternalWorkerStates();
        }

        private void ConnectionMonitor_OnConnectionChange(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            
            var (changedConnection, isConnected) = e.ChangedConnection;
            if(changedConnection.IsUpstream)
            {
                return; //we will get two reports, one from upstream, one from downstream, we selectively ignore upstream to not handle duplicates.
            }
            
            
            var changedInstanceName = changedConnection.Endpoint.RemoteInstanceNames.ElementAt(changedConnection.ShardId);
            
            var currentState = _workerStates.Get(changedInstanceName);
            switch(currentState)
            {
                case WorkerState.Launchable:
                case WorkerState.Launched:
                case WorkerState.Halted:
                case WorkerState.Restoring:
                    if(!isConnected)
                    {
                        currentState = WorkerState.Faulted;
                    }
                    break;
                
                case WorkerState.Faulted:
                    currentState = isConnected ? WorkerState.Halted : currentState;
                    //nothing to do
                    break;

                case WorkerState.Offline:
                    //can *only* transition out of this state through heatbeat messages (wait for vertex to be fully connected)
                    break;
            }

            if (currentState != _workerStates.Get(changedInstanceName))
            {
                Console.WriteLine($"{_vertexConfiguration.InstanceName} - State monitor: {changedInstanceName} now has status {currentState}");
                _workerStates[changedInstanceName] = currentState;
                EmitEventsIfGraphStateRequires();
            }
        }

        public void UpdateStateFromHeartBeat(string originInstanceName, WorkerStatusPayload statusPayload)
        {
            _ = statusPayload ?? throw new ArgumentNullException(nameof(statusPayload));
            
            //Console.WriteLine($"WorkerStateMonitor - heartbeat from {originInstanceName}. UpstreamConnected: {statusPayload.UpstreamFullyConnected}, DownstreamConnected: {statusPayload.DownstreamFullyConnected}, IsWorking: {statusPayload.DataProcessActive}.");

            var currentState = _workerStates.Get(originInstanceName);
            if(currentState == WorkerState.Offline)
            {
                var islaunchable = statusPayload.UpstreamFullyConnected && statusPayload.DownstreamFullyConnected;
                currentState = islaunchable ? WorkerState.Launchable : currentState;
            }
            
            if(currentState != _workerStates.Get(originInstanceName))
            {
                Console.WriteLine($"{_vertexConfiguration.InstanceName} - State monitor: {originInstanceName} now has status {currentState}");
                _workerStates[originInstanceName] = currentState;
                EmitEventsIfGraphStateRequires();
            }
            
        }

        /// <summary>
        /// Notifies local connection status change to worker instance<br/>
        /// This is a direct indication of the remote worker being alive or dead.
        /// </summary>
        /// <param name="originInstanceName"></param>
        /// <param name="isConnected"></param>
        public void UpdateStateFromConnectionMonitor(string originInstanceName, bool isConnected)
        {
            var currentState = _workerStates.Get(originInstanceName);
            if(currentState == WorkerState.Launched && !isConnected)
            {
                currentState = WorkerState.Faulted;
            }

            if (currentState == WorkerState.Faulted && isConnected)
            {
                currentState = WorkerState.Halted;
            }

            if (currentState != _workerStates.Get(originInstanceName))
            {
                Console.WriteLine($"{_vertexConfiguration.InstanceName} - State monitor: {originInstanceName} now has status {currentState}");
                _workerStates[originInstanceName] = currentState;
                EmitEventsIfGraphStateRequires();
            }
        }

        /// <summary>
        /// Notifies the state monitor that a worker has sent a restore response<br/>
        /// Used to determine if/when workers are ready to resume working.
        /// </summary>
        /// <param name="originInstanceName"></param>
        public void UpdateStateFromRestoreResponse(string originInstanceName)
        {
            //TODO: IF STATE NOT RESTORING --> ERROR?
            //TODO: IF STATE RESTORING && RESPONSE SAYS "IM DONE" --> LAUNCHABLE
            //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            EmitEventsIfGraphStateRequires();
        }

        /// <summary>
        /// Performs checks based on all the worker states in the graph.<br/>
        /// - Start data processing when ready<br/>
        /// - Halt data processing in face of a failure<br/>
        /// - Restore checkpoint when failure recovered<br/>
        /// </summary>
        private void EmitEventsIfGraphStateRequires()
        {
            //TODO IF ANY FAULTED --> HALT DOWNSTREAM
            if (_workerStates.Values.Any(s => s == WorkerState.Faulted))
            {
                //instruct halt to downstream operators
                foreach(var faultedWorker in _workerStates.Where(p => p.Value == WorkerState.Faulted))
                {
                    //TODO: HALT DOWNSTREAM OF FAULTED!
                    //-- something like this but recursive to get all downstream instance names
                    _graphConfiguration.InstanceConnections.Where(tuple => {
                        var (from, to) = tuple;
                        return from == faultedWorker.Key;
                    });
                    //TODO: UPDATE STATES OF WORKERS THAT ARE NOW HALTED
                    //TODO: EMIT EVENTS FOR WORKERS THAT JUST GOT HALTED (IF ALREADY HALTED DO NOT EMIT)
                }
            }

            //if any worker is currently restoring a checkpoint we must wait for it to notify the coordinator of restore completion
            if(_workerStates.Any(s => s.Value == WorkerState.Restoring))
            {
                return; //wait
            }

            //if all workers are either started or ready to start then we can start the launchables
            if(_workerStates.Values.All(s => s == WorkerState.Launchable || s == WorkerState.Launched))
            {
                //emit workersstart event with affected worker instanceNames.
                var launchableWorkers = _workerStates.Where(p => p.Value == WorkerState.Launchable).ToArray();
                OnWorkersStart.Invoke(launchableWorkers.Select(p => p.Key).ToArray());
                foreach(var worker in launchableWorkers)
                {
                    _workerStates.Remove(worker.Key);
                    _workerStates.Add(worker.Key, WorkerState.Launched);
                }
            }

            //TODO: IF ALL HALTED OR IF MIX OF LAUNCHED AND HALTED
            //      --> INSTRUCT RESTORE
            if (!_workerStates.Values.Any(s => s == WorkerState.Faulted) && _workerStates.Values.All(s => s == WorkerState.Launched || s == WorkerState.Halted))
            {

            }
        }

        private void InitializeInternalWorkerStates()
        {
            foreach(var instanceName in _graphConfiguration.InstanceNames)
            {
                _workerStates.Add(instanceName, WorkerState.Offline);
            }
        }
    }
}
