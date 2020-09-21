using BlackSP.Core.Monitors;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
using Serilog;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Coordination
{
    public class WorkerGraphStateManager
    {
        public enum State
        {
            /// <summary>
            /// State indicating that all workers are either halted or running
            /// </summary>
            Idle,
            /// <summary>
            /// State indicating that all workers are running
            /// </summary>
            Running,
            /// <summary>
            /// State indicating that one or more workers are in faulted state (reentrant in face of multiple failures)
            /// </summary>
            Faulted,
            /// <summary>
            /// State indicating that one or more workers are restoring checkpoints
            /// </summary>
            Restoring
        }

        public enum Trigger
        {
            /// <summary>
            /// Trigger indicating that a worker has reached a faulted state
            /// </summary>
            WorkerFaulted,
            /// <summary>
            /// Trigger indicating that a worker has reached a healthy state
            /// </summary>
            WorkerHealthy
        }

        
        public State CurrentState => _graphStateMachine.State;

        private readonly IEnumerable<string> _workerInstanceNames;
        private readonly WorkerStateManager.Factory _stateMachineFactory;
        private readonly ICheckpointService _checkpointService;
        private readonly ILogger _logger;
        private readonly IDictionary<string, WorkerStateManager> _workerStateManagers;
        private readonly StateMachine<State, Trigger> _graphStateMachine;

        private IRecoveryLine _preparedRecoveryLine;

        public WorkerGraphStateManager(WorkerStateManager.Factory stateMachineFactory, 
                                       ICheckpointService checkpointService, 
                                       IVertexGraphConfiguration graphConfiguration, 
                                       IVertexConfiguration vertexConfiguration,
                                       ILogger logger)
        {
            _ = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _workerInstanceNames = graphConfiguration.InstanceNames.Where(s => s != vertexConfiguration.InstanceName);
            _stateMachineFactory = stateMachineFactory ?? throw new ArgumentNullException(nameof(stateMachineFactory));
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workerStateManagers = new Dictionary<string, WorkerStateManager>();
            _graphStateMachine = new StateMachine<State, Trigger>(State.Idle);
            _preparedRecoveryLine = null;

            InitializeWorkerStateMachines();
            InitializeGraphStateMachine();
        }

        /// <summary>
        /// Configure graph state manager to listen to connection changes and fire network triggers on affected vertex state managers
        /// </summary>
        /// <param name="connectionMonitor"></param>
        public void ListenTo(ConnectionMonitor connectionMonitor)
        {
            _ = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChange;
        }

        public IEnumerable<WorkerStateManager> GetAllWorkerStateManagers() => _workerStateManagers.Values;

        public WorkerStateManager GetWorkerStateManager(string instanceName) => _workerStateManagers[instanceName];


        private void InitializeWorkerStateMachines()
        {
            foreach (var instanceName in _workerInstanceNames)
            {
                var workerStateMachine = _stateMachineFactory.Invoke(instanceName);
                _workerStateManagers.Add(instanceName, workerStateMachine);
                workerStateMachine.OnStateChange += WorkerStateMachine_OnStateChange;
            }
        }

        private void WorkerStateMachine_OnStateChange(string affectedInstanceName, WorkerState newState)
        {
            _logger.Information($"Worker {affectedInstanceName} transitioned to state: {newState}");
            if (newState == WorkerState.Faulted)
            {
                _graphStateMachine.Fire(Trigger.WorkerFaulted);
            }

            if(newState == WorkerState.Halted)
            {
                _graphStateMachine.Fire(Trigger.WorkerHealthy);
            }

            //... workers will automagically go back to halted state when they restart
            //... workers will automagically go back to halted state when checkpoint restore completes
        }

        private void ConnectionMonitor_OnConnectionChange(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            var (changedConnection, isConnected) = e.ChangedConnection;
            if (!changedConnection.IsUpstream)
            {
                return; //we will get two reports, one from upstream, one from downstream, selectively ignore downstream to not handle duplicates.
            }
            var changedInstanceName = changedConnection.Endpoint.RemoteInstanceNames.ElementAt(changedConnection.ShardId);
            var workerManager = this.GetWorkerStateManager(changedInstanceName);
            workerManager.FireTrigger(isConnected ? WorkerStateTrigger.NetworkConnected : WorkerStateTrigger.Failure); //Note: if we lose connection to the worker we assume it failed
        }

        private void InitializeGraphStateMachine()
        {
            Func<bool> allWorkersHaltedOrRunningGuard = () => AreAllWorkersInState(WorkerState.Halted, WorkerState.Running);
            Func<bool> isNoRecoveryLinePreparedGuard = () => !IsRecoveryLinePrepared();
            Func<bool> isRecoveryLinePreparedGuard = () => IsRecoveryLinePrepared();

            _graphStateMachine.Configure(State.Idle)
                .IgnoreIf(Trigger.WorkerHealthy, () => !allWorkersHaltedOrRunningGuard(), "Ignore while not all workers ready to start")
                .PermitIf(Trigger.WorkerHealthy, State.Running, Tuple.Create(isNoRecoveryLinePreparedGuard, "No recovery line must be prepared to start workers"), Tuple.Create(allWorkersHaltedOrRunningGuard, "All workers must be ready to start"))
                .PermitIf(Trigger.WorkerHealthy, State.Restoring, Tuple.Create(isRecoveryLinePreparedGuard, "Recovery line must be prepared to restore it"), Tuple.Create(allWorkersHaltedOrRunningGuard, "All workers must be ready to start"))
                .Permit(Trigger.WorkerFaulted, State.Faulted);
;
            _graphStateMachine.Configure(State.Running)
                .OnEntry(LaunchAllWorkers)
                .Permit(Trigger.WorkerFaulted, State.Faulted)
                .Ignore(Trigger.WorkerHealthy);

            _graphStateMachine.Configure(State.Restoring)
                .OnEntry(RecoverPreparedRecoveryLine)
                .Permit(Trigger.WorkerFaulted, State.Faulted)
                .PermitIf(Trigger.WorkerHealthy, State.Running, allWorkersHaltedOrRunningGuard)
                .IgnoreIf(Trigger.WorkerHealthy, () => !allWorkersHaltedOrRunningGuard(), "Ignore while there are still failed workers");

            _graphStateMachine.Configure(State.Faulted)
                .OnEntry(PrepareRecoveryLine)
                .PermitReentry(Trigger.WorkerFaulted)
                .PermitIf(Trigger.WorkerHealthy, State.Restoring, allWorkersHaltedOrRunningGuard)
                .IgnoreIf(Trigger.WorkerHealthy, () => !allWorkersHaltedOrRunningGuard(), "Ignore while there are still failed workers");

            _graphStateMachine.OnTransitioned(transition => {
                _logger.Information($"Graph transitioned to state: {transition.Destination}{(transition.IsReentry ? " (reentry)" : " ")} due to trigger {transition.Trigger}");
            });
        }

        #region state transition entry actions

        private void LaunchAllWorkers()
        {
            var haltedWorkers = _workerStateManagers.Values.Where(sm => sm.CurrentState == WorkerState.Halted);
            foreach (var workerManager in haltedWorkers)
            {
                workerManager.FireTrigger(WorkerStateTrigger.DataProcessorStart);
            }
        }

        private void PrepareRecoveryLine()
        {
            var failedInstances = _workerStateManagers.Values.Where(sm => sm.CurrentState == WorkerState.Faulted).Select(sm => sm.InstanceName);
            var t = Task.Run(async () =>
            {
                _preparedRecoveryLine = await _checkpointService.CalculateRecoveryLine(failedInstances).ConfigureAwait(false) 
                    ?? throw new Exception("Recovery line calculation yielded null, cannot continue");
                var workersToHalt = _workerStateManagers.Where(kv => _preparedRecoveryLine.AffectedWorkers.Contains(kv.Key))
                                    .Where(kv => kv.Value.CurrentState == WorkerState.Running)
                                    .Select(kv => kv.Value);

                foreach (var statemachine in workersToHalt)
                {
                    statemachine.FireTrigger(WorkerStateTrigger.DataProcessorHalt);
                }
            });
            t.Wait(); //wait for async operation to complete before returning
        }

        private void RecoverPreparedRecoveryLine()
        {
            _ = _preparedRecoveryLine ?? throw new InvalidOperationException("Cannot recover recovery line as none had been prepared");
            foreach(var name in _preparedRecoveryLine.AffectedWorkers)
            {
                _workerStateManagers[name].FireTrigger(WorkerStateTrigger.CheckpointRestoreStart, _preparedRecoveryLine.RecoveryMap[name]);
            }
            _preparedRecoveryLine = null;
        }

        #endregion

        #region state transition guards

        private bool IsRecoveryLinePrepared()
        {
            return _preparedRecoveryLine != null;
        }

        private bool AreAllWorkersInState(params WorkerState[] states)
        {
            var allInState = true;
            foreach(var workerManager in _workerStateManagers.Values)
            {
                allInState = allInState && states.Contains(workerManager.CurrentState);
            }
            return allInState;
        }

        private bool IsAnyWorkerInState(params WorkerState[] states)
        {
            foreach (var workerManager in _workerStateManagers.Values)
            {
                if(states.Contains(workerManager.CurrentState))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
