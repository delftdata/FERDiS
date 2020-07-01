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
                        //TODO: HALT downstream?
                        //-- something like this but recursive to get all downstream instance names
                        _graphConfiguration.InstanceConnections.Where(tuple => { 
                            var (from, to) = tuple; 
                            return from == changedInstanceName; 
                        });
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
            //update worker state
            _workerStates.Remove(changedInstanceName);
            _workerStates.Add(changedInstanceName, currentState);
            
            
            //check each worker state?

            
            //- if worker state == launchable
            //-- is not connected --> faulted + HALT DOWNSTREAM?
            //-- is connected --> continue and check if all connected && launchable || launched (upstream of failure can be launched)
            //--- if all connected, launch all launchable

            //- if worker state == launched
            //-- is not connected --> faulted + HALT DOWNSTREAM?
            //-- is connected --> wait

            //- if worker state == faulted
            //-- is not connected --> wait
            //-- is connected --> halted + INITIATE ROLLBACK?

            //- if worker state == halted
            //-- is not connected --> faulted + HALT DOWNSTREAM?
            //-- is connected --> wait

            //- if worker state == restoring
            //-- is not connected --> faulted + HALT DOWNSTREAM?
            //-- is connected --> wait
        }

        public void UpdateStateFromReport(string originInstanceName, WorkerStatusPayload statusPayload)
        {
            _ = statusPayload ?? throw new ArgumentNullException(nameof(statusPayload));

            var currentState = _workerStates.Get(originInstanceName);
            if(currentState == WorkerState.Offline)
            {
                var islaunchable = statusPayload.UpstreamFullyConnected && statusPayload.DownstreamFullyConnected;
                currentState = islaunchable ? WorkerState.Launchable : currentState;
            }
            //update worker state
            _workerStates.Remove(originInstanceName);
            _workerStates.Add(originInstanceName, currentState);
            
            
            //TODO: IF ALL LAUNCHABLE --> START DATA PROCESS (MOVE TO LAUNCHED STATUS)
            //OR IF MIX OF LAUNCHED AND LAUNCHABLE --> START LAUNCHABLES
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

            //TODO: IF ALL HALTED --> INSTRUCT RESTORE

            _workerStates.Remove(originInstanceName);
            _workerStates.Add(originInstanceName, currentState);
        }

        /// <summary>
        /// Notifies the state monitor that a worker has sent a restore response<br/>
        /// Used to determine if/when the entire worker graph is ready to start working again.
        /// </summary>
        /// <param name="originInstanceName"></param>
        public void UpdateStateFromRestoreResponse(string originInstanceName)
        {
            //TODO: IF STATE NOT RESTORING --> ERROR
            //TODO: IF STATE RESTORING && RESPONSE SAYS "IM DONE" --> LAUNCHABLE

            //TODO: IF ALL LAUNCHABLE --> START DATA PROCESS (MOVE TO LAUNCHED STATUS) (ALSO REQUIRED IN OTHER METHODS!!)
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
