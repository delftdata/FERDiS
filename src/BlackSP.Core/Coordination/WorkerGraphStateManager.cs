using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private readonly IEnumerable<string> _workerInstanceNames;
        private readonly WorkerStateManager.Factory _stateMachineFactory;
        private readonly ICheckpointService _checkpointService;
        private readonly IDictionary<string, WorkerStateManager> _workerStateMachines;
        private readonly StateMachine<State, Trigger> _graphStateMachine;

        public delegate void StateChangeEvent();
        public event StateChangeEvent Lol;

        public WorkerGraphStateManager(IEnumerable<string> workerInstanceNames, WorkerStateManager.Factory stateMachineFactory, ICheckpointService checkpointService)
        {
            _workerInstanceNames = workerInstanceNames ?? throw new ArgumentNullException(nameof(workerInstanceNames));
            _stateMachineFactory = stateMachineFactory ?? throw new ArgumentNullException(nameof(stateMachineFactory));
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _workerStateMachines = new Dictionary<string, WorkerStateManager>();
            _graphStateMachine = new StateMachine<State, Trigger>(State.Idle);
            
            InitializeWorkerStateMachines();
            InitializeGraphStateMachine();
        }

        private void InitializeWorkerStateMachines()
        {
            foreach (var instanceName in _workerInstanceNames)
            {
                var workerStateMachine = _stateMachineFactory.Invoke(instanceName);
                _workerStateMachines.Add(instanceName, workerStateMachine);
                workerStateMachine.OnStateChange += WorkerStateMachine_OnStateChange;
            }
        }

        private void WorkerStateMachine_OnStateChange(string affectedInstanceName, WorkerStateManager.State newState)
        {
            //if any faulted (even when already other faulted)
            //- prepare recovery line (or overwrite if already present)
            //- halt workers that are affected (and in running state)
            //- wait
           
            if(newState == WorkerStateManager.State.Faulted)
            {
                _graphStateMachine.Fire(Trigger.WorkerFaulted);
            }

            if(newState == WorkerStateManager.State.Halted)
            {
                _graphStateMachine.Fire(Trigger.WorkerHealthy);
            }

            //... workers will automagically go back to halted state when they restart

            //when no faulted and a subset is halted..
            //if recovery line prepared, transition to restoring state
            //if no recovery line prepared, transition to running

            //when restoring
            //- trigger restore in every worker state machine with checkpoint ID
            //- remove prepared recovery line

            //... workers will automagically go back to halted state when checkpoint restore completes
        }

        private void InitializeGraphStateMachine()
        {
            _graphStateMachine.Configure(State.Idle)
                .PermitIf(Trigger.WorkerHealthy, State.Running, () => !IsRecoveryLinePrepared())
                .PermitIf(Trigger.WorkerHealthy, State.Restoring, IsRecoveryLinePrepared)
                .IgnoreIf(Trigger.WorkerHealthy, () => !AreAllWorkersInState(WorkerStateManager.State.Halted, WorkerStateManager.State.Running))
                .Permit(Trigger.WorkerFaulted, State.Faulted);

            _graphStateMachine.Configure(State.Running)
                .OnEntry(LaunchAllWorkers)
                .Permit(Trigger.WorkerFaulted, State.Faulted);

            _graphStateMachine.Configure(State.Restoring)
                .OnEntry(RecoverPreparedRecoveryLine)
                .Permit(Trigger.WorkerFaulted, State.Faulted);

            _graphStateMachine.Configure(State.Faulted)
                .OnEntry(PrepareRecoveryLine)
                .PermitReentry(Trigger.WorkerFaulted)
                .PermitIf(Trigger.WorkerHealthy, State.Idle)
                .IgnoreIf(Trigger.WorkerHealthy, () => !AreAllWorkersInState(WorkerStateManager.State.Halted, WorkerStateManager.State.Running));
        }

        #region state transition entry actions

        private void LaunchAllWorkers()
        {
            foreach(var workerManager in _workerStateMachines.Values)
            {
                workerManager.FireTrigger(WorkerStateManager.Trigger.DataProcessorStart);
            }
        }

        private void PrepareRecoveryLine()
        {
            //_checkpointService.CalculateRecoveryLine ...?
            
            throw new NotImplementedException();
            //TODO: halt affected workers (downstream/all?)
        }

        private void RecoverPreparedRecoveryLine()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region state transition guards

        private bool IsRecoveryLinePrepared()
        {
            throw new NotImplementedException();
        }

        private bool AreAllWorkersInState(params WorkerStateManager.State[] states)
        {
            var allInState = true;
            foreach(var workerManager in _workerStateMachines.Values)
            {
                allInState = allInState && states.Contains(workerManager.CurrentState);
            }
            return allInState;
        }

        private bool AreAnyWorkersInState(params WorkerStateManager.State[] states)
        {
            foreach (var workerManager in _workerStateMachines.Values)
            {
                if(states.Contains(workerManager.CurrentState))
                {
                    return true;
                }
            }
            return false;
        }

        //private bool 

        #endregion
    }
}
