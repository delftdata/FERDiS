using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
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

        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="workerInstanceNames"></param>
        /// <returns></returns>
        public delegate WorkerGraphStateManager Factory(IEnumerable<string> workerInstanceNames);

        public IEnumerable<WorkerStateManager> WorkerStateManagers => _workerStateManagers.Values;
        public State CurrentState => _graphStateMachine.State;

        private readonly IEnumerable<string> _workerInstanceNames;
        private readonly WorkerStateManager.Factory _stateMachineFactory;
        private readonly ICheckpointService _checkpointService;
        private readonly IDictionary<string, WorkerStateManager> _workerStateManagers;
        private readonly StateMachine<State, Trigger> _graphStateMachine;

        private IRecoveryLine _preparedRecoveryLine;

        public WorkerGraphStateManager(IEnumerable<string> workerInstanceNames, WorkerStateManager.Factory stateMachineFactory, ICheckpointService checkpointService)
        {
            _workerInstanceNames = workerInstanceNames ?? throw new ArgumentNullException(nameof(workerInstanceNames));
            _stateMachineFactory = stateMachineFactory ?? throw new ArgumentNullException(nameof(stateMachineFactory));
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            
            _workerStateManagers = new Dictionary<string, WorkerStateManager>();
            _graphStateMachine = new StateMachine<State, Trigger>(State.Idle);
            _preparedRecoveryLine = null;

            InitializeWorkerStateMachines();
            InitializeGraphStateMachine();
        }

        private void InitializeWorkerStateMachines()
        {
            foreach (var instanceName in _workerInstanceNames)
            {
                var workerStateMachine = _stateMachineFactory.Invoke(instanceName);
                _workerStateManagers.Add(instanceName, workerStateMachine);
                workerStateMachine.OnStateChange += WorkerStateMachine_OnStateChange;
            }
        }

        private void WorkerStateMachine_OnStateChange(string affectedInstanceName, WorkerStateManager.State newState)
        {
            if(newState == WorkerStateManager.State.Faulted)
            {
                _graphStateMachine.Fire(Trigger.WorkerFaulted);
            }

            if(newState == WorkerStateManager.State.Halted)
            {
                _graphStateMachine.Fire(Trigger.WorkerHealthy);
            }

            //... workers will automagically go back to halted state when they restart
            //... workers will automagically go back to halted state when checkpoint restore completes
        }

        private void InitializeGraphStateMachine()
        {
            Func<bool> allWorkersHaltedOrRunningGuard = () => AreAllWorkersInState(WorkerStateManager.State.Halted, WorkerStateManager.State.Running);
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
                .Permit(Trigger.WorkerFaulted, State.Faulted);

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
        }

        #region state transition entry actions

        private void LaunchAllWorkers()
        {
            var haltedWorkers = _workerStateManagers.Values.Where(sm => sm.CurrentState == WorkerStateManager.State.Halted);
            foreach (var workerManager in haltedWorkers)
            {
                workerManager.FireTrigger(WorkerStateManager.Trigger.DataProcessorStart);
            }
        }

        private void PrepareRecoveryLine()
        {
            var failedInstances = _workerStateManagers.Values.Where(sm => sm.CurrentState == WorkerStateManager.State.Faulted).Select(sm => sm.InstanceName);
            var t = Task.Run(async () =>
            {
                _preparedRecoveryLine = await _checkpointService.CalculateRecoveryLine(failedInstances).ConfigureAwait(false) 
                    ?? throw new Exception("Recovery line calculation yielded null, cannot continue");
                var workersToHalt = _workerStateManagers.Where(kv => _preparedRecoveryLine.AffectedWorkers.Contains(kv.Key))
                                    .Where(kv => kv.Value.CurrentState == WorkerStateManager.State.Running)
                                    .Select(kv => kv.Value);

                foreach (var statemachine in workersToHalt)
                {
                    statemachine.FireTrigger(WorkerStateManager.Trigger.DataProcessorHalt);
                }
            });
            t.Wait(); //wait for async operation to complete before returning
        }

        private void RecoverPreparedRecoveryLine()
        {
            foreach(var name in _preparedRecoveryLine.AffectedWorkers)
            {
                _workerStateManagers[name].FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreStart, _preparedRecoveryLine.RecoveryMap[name]);
            }
            _preparedRecoveryLine = null;
        }

        #endregion

        #region state transition guards

        private bool IsRecoveryLinePrepared()
        {
            return _preparedRecoveryLine != null;
        }

        private bool AreAllWorkersInState(params WorkerStateManager.State[] states)
        {
            var allInState = true;
            foreach(var workerManager in _workerStateManagers.Values)
            {
                allInState = allInState && states.Contains(workerManager.CurrentState);
            }
            return allInState;
        }

        private bool IsAnyWorkerInState(params WorkerStateManager.State[] states)
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
