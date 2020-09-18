using Serilog;
using Stateless;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Coordination
{
    public enum WorkerState
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

    public enum WorkerStateTrigger
    {
        Failure,
        NetworkConnected,
        NetworkDisconnected,
        DataProcessorStart,
        DataProcessorHalt,
        CheckpointRestoreStart,
        CheckpointRestoreCompleted
    }

    public class WorkerStateManager
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="workerInstanceName"></param>
        /// <returns></returns>
        public delegate WorkerStateManager Factory(string workerInstanceName);

        public string InstanceName { get; private set; }
        public WorkerState CurrentState => _stateMachine.State;
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
        
        public delegate void StateChangeEvent(string affectedInstanceName, WorkerState newState);

        private Guid restoringCheckpointId;

        private readonly ILogger _logger;
        private readonly StateMachine<WorkerState, WorkerStateTrigger> _stateMachine;
        private readonly StateMachine<WorkerState, WorkerStateTrigger>.TriggerWithParameters<Guid> _checkpointRestoreInitiationTrigger;
        private readonly StateMachine<WorkerState, WorkerStateTrigger>.TriggerWithParameters<Guid> _checkpointRestoreCompletionTrigger;

        public WorkerStateManager(string workerInstanceName, ILogger logger)
        {
            InstanceName = workerInstanceName ?? throw new ArgumentNullException(nameof(workerInstanceName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            restoringCheckpointId = default;

            var machine = new StateMachine<WorkerState, WorkerStateTrigger>(WorkerState.Offline);
            _checkpointRestoreInitiationTrigger = machine.SetTriggerParameters<Guid>(WorkerStateTrigger.CheckpointRestoreStart);
            _checkpointRestoreCompletionTrigger = machine.SetTriggerParameters<Guid>(WorkerStateTrigger.CheckpointRestoreCompleted);
            _stateMachine = ConfigureStateMachine(machine);
            machine.OnTransitioned(OnStateTransition);
        }

        #region statemachine guards & actions

        private StateMachine<WorkerState, WorkerStateTrigger> ConfigureStateMachine(StateMachine<WorkerState, WorkerStateTrigger> machine)
        {
            machine.Configure(WorkerState.Offline)
                .Permit(WorkerStateTrigger.NetworkConnected, WorkerState.Halted);

            machine.Configure(WorkerState.Halted)
                .OnEntryFrom(WorkerStateTrigger.DataProcessorHalt, OnHaltDataProcessor, "Processor actively requested to halt")
                .OnEntryFrom(WorkerStateTrigger.NetworkDisconnected, OnHaltDataProcessor, "Worker has network disconnect, halt processor")
                .Permit(WorkerStateTrigger.DataProcessorStart, WorkerState.Running)
                .PermitIf(_checkpointRestoreInitiationTrigger, WorkerState.Recovering, EnsureValidCheckpointId, "Checkpoint validity guard")
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted)
                .Ignore(WorkerStateTrigger.DataProcessorHalt)
                .Ignore(WorkerStateTrigger.NetworkConnected)
                .Ignore(WorkerStateTrigger.NetworkDisconnected); //TODO: consider if this trigger is ignorable here or should yield Failed state

            machine.Configure(WorkerState.Running)
                .OnEntry(OnStartDataProcessor)
                .Permit(WorkerStateTrigger.NetworkDisconnected, WorkerState.Halted)
                .Permit(WorkerStateTrigger.DataProcessorHalt, WorkerState.Halted)
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted)
                .Ignore(WorkerStateTrigger.DataProcessorStart)
                .Ignore(WorkerStateTrigger.NetworkConnected);

            machine.Configure(WorkerState.Recovering)
                .OnEntryFrom(_checkpointRestoreInitiationTrigger, OnStartCheckpointRestore)
                .PermitReentryIf(_checkpointRestoreInitiationTrigger, EnsureValidCheckpointId, "CheckpointId validity guard (reentry)")
                .OnExit(CompleteRestoringCheckpoint)
                .PermitIf(_checkpointRestoreCompletionTrigger, WorkerState.Halted, EnsureCorrectCheckpointRestored, "Expected checkpointId for completion guard")
                .IgnoreIf(_checkpointRestoreCompletionTrigger, (id) => !EnsureCorrectCheckpointRestored(id), "Ignore if checkpointId is not as expected guard")
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted);

            machine.Configure(WorkerState.Faulted)
                .Permit(WorkerStateTrigger.NetworkConnected, WorkerState.Halted);

            return machine;
        }

        private void OnStateTransition(StateMachine<WorkerState, WorkerStateTrigger>.Transition transition)
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
        public void FireTrigger(WorkerStateTrigger trigger)
        {
            if(trigger == WorkerStateTrigger.CheckpointRestoreStart || trigger == WorkerStateTrigger.CheckpointRestoreCompleted)
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
        public void FireTrigger(WorkerStateTrigger trigger, Guid checkpointId)
        {
            if (trigger != WorkerStateTrigger.CheckpointRestoreStart && trigger != WorkerStateTrigger.CheckpointRestoreCompleted)
            {
                throw new ArgumentException("Only checkpoint related triggers take a Guid argument", nameof(trigger));
            }

            var triggerWithParam = trigger == WorkerStateTrigger.CheckpointRestoreStart ? _checkpointRestoreInitiationTrigger : _checkpointRestoreCompletionTrigger;
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
