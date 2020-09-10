using Serilog;
using Stateless;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Coordination
{
    public class WorkerStateManager
    {

        public enum State
        {
            /// <summary>
            /// Initial state of a worker, will never be reached after leaving this state
            /// </summary>
            Offline,
            /// <summary>
            /// The worker is ready to start processing data, but is not doing so
            /// </summary>
            Halted,
            /// <summary>
            /// The worker is processing data
            /// </summary>
            Running,
            /// <summary>
            /// The worker is restoring its internal state
            /// </summary>
            Recovering,
            /// <summary>
            /// The worker was healthy before but is now offline due to a fault
            /// </summary>
            Faulted
        }

        public enum Trigger
        {
            Failure,
            NetworkConnected,
            NetworkDisconnected,
            DataProcessorStart,
            DataProcessorHalt,
            CheckpointRestoreStart,
            CheckpointRestoreCompleted
        }

        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="workerInstanceName"></param>
        /// <returns></returns>
        public delegate WorkerStateManager Factory(string workerInstanceName);

        public string InstanceName { get; private set; }
        public State CurrentState => _stateMachine.State;
        public Guid RestoringCheckpointId => restoringCheckpointId;

        /// <summary>
        /// Event that fires whenever a state change has happened
        /// </summary>
        public event StateChangeEvent OnStateChange;

        /// <summary>
        /// Event that fires whenever a state change has happened of which the worker should receive a message/notification.
        /// For example: a state change from offline to halted does not require notifying the worker. But halted to started requires a 'start' message to be sent.
        /// </summary>
        public event StateChangeEvent OnStateChangeNotificationRequired;
        
        public delegate void StateChangeEvent(string affectedInstanceName, State newState);

        private Guid restoringCheckpointId;

        private readonly ILogger _logger;
        private readonly StateMachine<State, Trigger> _stateMachine;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _checkpointRestoreInitiationTrigger;
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<Guid> _checkpointRestoreCompletionTrigger;

        public WorkerStateManager(string workerInstanceName, ILogger logger)
        {
            InstanceName = workerInstanceName ?? throw new ArgumentNullException(nameof(workerInstanceName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            restoringCheckpointId = default;

            var machine = new StateMachine<State, Trigger>(State.Offline);
            _checkpointRestoreInitiationTrigger = machine.SetTriggerParameters<Guid>(Trigger.CheckpointRestoreStart);
            _checkpointRestoreCompletionTrigger = machine.SetTriggerParameters<Guid>(Trigger.CheckpointRestoreCompleted);
            _stateMachine = ConfigureStateMachine(machine);
            machine.OnTransitioned(OnStateTransition);
        }

        #region statemachine guards & actions

        private StateMachine<State, Trigger> ConfigureStateMachine(StateMachine<State, Trigger> machine)
        {
            machine.Configure(State.Offline)
                .Permit(Trigger.NetworkConnected, State.Halted);

            machine.Configure(State.Halted)
                .OnEntryFrom(Trigger.DataProcessorHalt, OnHaltDataProcessor, "Processor actively requested to halt")
                .OnEntryFrom(Trigger.NetworkDisconnected, OnHaltDataProcessor, "Worker has network disconnect, halt processor")
                .Permit(Trigger.DataProcessorStart, State.Running)
                .PermitIf(_checkpointRestoreInitiationTrigger, State.Recovering, EnsureValidCheckpointId, "Checkpoint validity guard")
                .Permit(Trigger.Failure, State.Faulted)
                .Ignore(Trigger.DataProcessorHalt);

            machine.Configure(State.Running)
                .OnEntry(OnStartDataProcessor)
                .Permit(Trigger.NetworkDisconnected, State.Halted)
                .Permit(Trigger.DataProcessorHalt, State.Halted)
                .Permit(Trigger.Failure, State.Faulted)
                .Ignore(Trigger.DataProcessorStart);

            machine.Configure(State.Recovering)
                .OnEntryFrom(_checkpointRestoreInitiationTrigger, OnStartCheckpointRestore)
                .PermitReentryIf(_checkpointRestoreInitiationTrigger, EnsureValidCheckpointId, "CheckpointId validity guard (reentry)")
                .OnExit(CompleteRestoringCheckpoint)
                .PermitIf(_checkpointRestoreCompletionTrigger, State.Halted, EnsureCorrectCheckpointRestored, "Expected checkpointId for completion guard")
                .Permit(Trigger.Failure, State.Faulted);

            machine.Configure(State.Faulted)
                .Permit(Trigger.NetworkConnected, State.Halted);

            return machine;
        }

        private void OnStateTransition(StateMachine<State, Trigger>.Transition transition)
        {
            if(transition.IsReentry)
            {
                return;
            }
            OnStateChange?.Invoke(InstanceName, transition.Destination);
        }

        private bool EnsureCorrectCheckpointRestored(Guid checkpointId)
        {
            return restoringCheckpointId == checkpointId;
        }

        private bool EnsureValidCheckpointId(Guid checkpointId)
        {
            return checkpointId != default;
        }

        private void OnStartCheckpointRestore(Guid checkpointId)
        {
            restoringCheckpointId = checkpointId;
            OnStateChangeNotificationRequired?.Invoke(InstanceName, CurrentState);
        }

        private void CompleteRestoringCheckpoint()
        {
            restoringCheckpointId = default;
            //no notification required
        }

        private void OnStartDataProcessor()
        {
            OnStateChangeNotificationRequired?.Invoke(InstanceName, _stateMachine.State);
        }

        private void OnHaltDataProcessor()
        {
            OnStateChangeNotificationRequired?.Invoke(InstanceName, _stateMachine.State);
        }
        #endregion

        /// <summary>
        /// Notify the statemachine of a trigger, may cause state change
        /// </summary>
        public void FireTrigger(Trigger trigger)
        {
            if(trigger == Trigger.CheckpointRestoreStart || trigger == Trigger.CheckpointRestoreCompleted)
            {
                throw new ArgumentException("Checkpoint related triggers require Guid argument", nameof(trigger));
            }
            
            try
            {
                _stateMachine.Fire(trigger);
            }
            catch (InvalidOperationException e)
            {
                _logger.Debug(e, $"Invalid worker state transition using trigger {trigger}");
                throw;
            }
        }

        /// <summary>
        /// Notify the statemachine of a trigger that takes a checkpoint identifier as argument, may cause state change
        /// </summary>
        public void FireTrigger(Trigger trigger, Guid checkpointId)
        {
            if (trigger != Trigger.CheckpointRestoreStart && trigger != Trigger.CheckpointRestoreCompleted)
            {
                throw new ArgumentException("Only checkpoint related triggers take a Guid argument", nameof(trigger));
            }

            var triggerWithParam = trigger == Trigger.CheckpointRestoreStart ? _checkpointRestoreInitiationTrigger : _checkpointRestoreCompletionTrigger;
            try
            {
                _stateMachine.Fire(triggerWithParam, checkpointId);
            }
            catch(InvalidOperationException e)
            {
                _logger.Debug(e, $"Invalid worker state transition using trigger {trigger}");
                throw;
            }
        }

    }
}
