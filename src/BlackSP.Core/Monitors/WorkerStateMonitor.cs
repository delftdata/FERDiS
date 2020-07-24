using BlackSP.Core.Extensions;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.Models;
using Serilog;
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
        private readonly ILogger _logger;

        private readonly Dictionary<string, WorkerState> _workerStates;

        public delegate void AffectedWorkersEventHandler(IEnumerable<string> affectedInstanceNames);
        public event AffectedWorkersEventHandler OnWorkersStart;
        public event AffectedWorkersEventHandler OnWorkersRestore;
        public event AffectedWorkersEventHandler OnWorkersHalt;

        public WorkerStateMonitor(IVertexGraphConfiguration graphConfiguration,
                                  IVertexConfiguration vertexConfiguration, 
                                  ConnectionMonitor connectionMonitor,
                                  ILogger logger)
        {
            //assumption: current vertexconfiguration is that of coordinator --> only has control channels to all vertices in system
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChange;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _workerStates = new Dictionary<string, WorkerState>();
            InitializeInternalWorkerStates();
        }

        /// <summary>
        /// Interpret connection changes as direct indicators of worker health, this is then used to update internal state<br/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectionMonitor_OnConnectionChange(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            var (changedConnection, isConnected) = e.ChangedConnection;
            if(changedConnection.IsUpstream)
            {
                return; //we will get two reports, one from upstream, one from downstream, selectively ignore upstream to not handle duplicates.
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
                    break;

                case WorkerState.Offline:
                    //can *only* transition out of this state through heatbeat messages (wait for vertex to be fully connected)
                    break;
            }

            if (currentState != _workerStates.Get(changedInstanceName))
            {
                _logger.Debug($"State monitor: {changedInstanceName} now has status {currentState} (connection)");
                _workerStates[changedInstanceName] = currentState;
                EmitEvents();
            }
        }

        public void NotifyWorkerHeartbeat(string originInstanceName, WorkerStatusPayload statusPayload)
        {
            _ = statusPayload ?? throw new ArgumentNullException(nameof(statusPayload));
            
            var currentState = _workerStates.Get(originInstanceName);
            if(currentState == WorkerState.Offline)
            {
                var islaunchable = statusPayload.UpstreamFullyConnected && statusPayload.DownstreamFullyConnected;
                currentState = islaunchable ? WorkerState.Launchable : currentState;
            }
            
            if(currentState != _workerStates.Get(originInstanceName))
            {
                _logger.Debug($"State monitor: {originInstanceName} now has status {currentState} (hearbeat)");
                _workerStates[originInstanceName] = currentState;
                EmitEvents();
            }
        }

        /// <summary>
        /// Notifies the state monitor that a worker has sent a restore response<br/>
        /// Used to determine if/when workers are ready to resume working.
        /// </summary>
        /// <param name="originInstanceName"></param>
        public void NotifyRestoreCompletion(string originInstanceName)
        {
            var currentState = _workerStates.Get(originInstanceName);
            if(currentState != WorkerState.Restoring)
            {
                var msg = $"State monitor received restore completion of worker {originInstanceName} which was not restoring, throwing exception";
                _logger.Warning(msg);
                throw new Exception(msg);
            }
            _workerStates[originInstanceName] = WorkerState.Launchable;
            EmitEvents();
        }

        /// <summary>
        /// Performs checks based on all the worker states in the graph.<br/>
        /// - Start data processing when ready<br/>
        /// - Halt data processing in face of a failure<br/>
        /// - Restore checkpoint when failure recovered<br/>
        /// </summary>
        private void EmitEvents()
        {
            try
            {
                PerformWorkerHaltChecks();
            }
            catch (Exception e)
            {
                var x = 1;
            }

            //if any worker is currently restoring a checkpoint we must wait for it to notify the coordinator of restore completion
            if (_workerStates.Any(s => s.Value == WorkerState.Restoring))
            {
                return; //a restore is in progress, wait for all workers to leave the restoring state
            }
            
            // there are no faulted workers..
            // all workers are either launched or halted 
            // therefore: workers have restarted and are awaiting instructions
            if (!_workerStates.Values.Any(s => s == WorkerState.Faulted) && _workerStates.Values.All(s => s == WorkerState.Launched || s == WorkerState.Halted))
            {
                //TODO: run tests and check who is responsible for figuring out the actual recovery line?
                var workersToRestore = _workerStates.Where(t => t.Value == WorkerState.Halted).Select(t => t.Key).ToArray();
                foreach(var instance in workersToRestore)
                {
                    _workerStates[instance] = WorkerState.Restoring;
                }
                OnWorkersRestore.Invoke(workersToRestore);
            }
            
            //if all workers are either started or ready to start then we can start the launchables
            if(_workerStates.Values.All(s => s == WorkerState.Launchable || s == WorkerState.Launched))
            {
                //emit workersstart event with affected worker instanceNames.
                var launchableWorkers = _workerStates.Where(p => p.Value == WorkerState.Launchable).ToArray();
                foreach(var worker in launchableWorkers)
                {
                    _workerStates[worker.Key] = WorkerState.Launched;
                }
                OnWorkersStart.Invoke(launchableWorkers.Select(p => p.Key).ToArray());
            }

            
        }

        //TODO: consider creating abstract base class with two implementations of method below
        //      1. as is: with downstream halt (uncoordinated approaches)
        //      2. with full graph halt (coordinated approach)
        private void PerformWorkerHaltChecks()
        {
            var newlyHalted = new List<string>();
            foreach (var faultedWorker in _workerStates.Where(p => p.Value == WorkerState.Faulted).ToArray())
            {
                var severedInstances = _graphConfiguration.GetAllInstancesDownstreamOf(faultedWorker.Key);
                foreach (var instance in severedInstances)
                {
                    var currentState = _workerStates.Get(instance);
                    if (currentState != WorkerState.Halted && currentState != WorkerState.Faulted) //if not halted or faulted, change state to halted.
                    {
                        _workerStates[instance] = WorkerState.Halted;
                        _logger.Debug($"State monitor: {instance} now has status {_workerStates[instance]} (downstream)");
                        newlyHalted.Add(instance);
                    }
                }
            }

            if (newlyHalted.Any())
            {   //signal state change (important difference: here halted is a state reached through coordinator instruction, not through detection)
                OnWorkersHalt.Invoke(newlyHalted);
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
