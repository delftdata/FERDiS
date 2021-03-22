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
        Halting, Halted,
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
        Startup,
        DataProcessorStart,
        DataProcessorHalt,
        DataProcessorHaltCompleted,
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
        public Guid RestoringCheckpointId { get; private set; }
        public (string[], string[]) DataProcessorHaltArgs { get; private set; }
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


        private readonly ILogger _logger;
        private readonly StateMachine<WorkerState, WorkerStateTrigger> _stateMachine;
        private readonly StateMachine<WorkerState, WorkerStateTrigger>.TriggerWithParameters<Guid> _checkpointRestoreInitiationTrigger;
        private readonly StateMachine<WorkerState, WorkerStateTrigger>.TriggerWithParameters<Guid> _checkpointRestoreCompletionTrigger;
        private readonly StateMachine<WorkerState, WorkerStateTrigger>.TriggerWithParameters<(string[], string[])> _dataProcessorHaltTrigger;

        private StateMachine<WorkerState, WorkerStateTrigger>.Transition lastTransition;

        public WorkerStateManager(string workerInstanceName, ILogger logger)
        {
            InstanceName = workerInstanceName ?? throw new ArgumentNullException(nameof(workerInstanceName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RestoringCheckpointId = default;

            var machine = new StateMachine<WorkerState, WorkerStateTrigger>(WorkerState.Offline);
            _checkpointRestoreInitiationTrigger = machine.SetTriggerParameters<Guid>(WorkerStateTrigger.CheckpointRestoreStart);
            _checkpointRestoreCompletionTrigger = machine.SetTriggerParameters<Guid>(WorkerStateTrigger.CheckpointRestoreCompleted);
            _dataProcessorHaltTrigger = machine.SetTriggerParameters<(string[], string[])>(WorkerStateTrigger.DataProcessorHalt);
            _stateMachine = ConfigureStateMachine(machine);
            
            machine.OnTransitioned(transition => lastTransition = transition);
        }

        /// <summary>
        /// Notify the statemachine of a trigger, may cause state change
        /// </summary>
        public void FireTrigger(WorkerStateTrigger trigger)
        {
            if (trigger == WorkerStateTrigger.CheckpointRestoreStart || trigger == WorkerStateTrigger.CheckpointRestoreCompleted)
            {
                throw new ArgumentException("Checkpoint related triggers require Guid argument", nameof(trigger));
            }
            else if(trigger == WorkerStateTrigger.DataProcessorHalt)
            {
                throw new ArgumentException("Data processor halt trigger requires (string[], string[]) argument", nameof(trigger));
            }

            try
            {
                _stateMachine.Fire(trigger);
                OnStateTransition(lastTransition);
            }
            catch (InvalidOperationException e)
            {
                _logger.Warning(e, $"Invalid statemachine transition in worker {InstanceName} caused by trigger {trigger} in state {CurrentState}");
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
                OnStateTransition(lastTransition);
            }
            catch (InvalidOperationException e)
            {
                _logger.Debug(e, $"Invalid worker state transition using trigger {trigger}");
                throw;
            }
        }

        /// <summary>
        /// Notify the statemachine of a trigger, may cause state change
        /// </summary>
        public void FireTrigger(WorkerStateTrigger trigger, (string[], string[]) upAndDownstreamHaltedInstances)
        {
            if (trigger != WorkerStateTrigger.DataProcessorHalt)
            {
                throw new ArgumentException("Only data processor halt trigger requires (string[],string[]) argument", nameof(trigger));
            }

            try
            {
                _stateMachine.Fire(_dataProcessorHaltTrigger, upAndDownstreamHaltedInstances);
                OnStateTransition(lastTransition);
            }
            catch (InvalidOperationException e)
            {
                _logger.Warning(e, $"Invalid statemachine transition in worker {InstanceName} caused by trigger {trigger} in state {CurrentState}");
                throw;
            }
        }

        #region statemachine guards & actions

        private StateMachine<WorkerState, WorkerStateTrigger> ConfigureStateMachine(StateMachine<WorkerState, WorkerStateTrigger> machine)
        {
            machine.Configure(WorkerState.Offline)
                .Permit(WorkerStateTrigger.Startup, WorkerState.Halted);

            machine.Configure(WorkerState.Halting)
                .OnEntryFrom(_dataProcessorHaltTrigger, OnHaltDataProcessor, "Processor actively requested to halt")
                .Permit(WorkerStateTrigger.DataProcessorHaltCompleted, WorkerState.Halted)
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted);

            machine.Configure(WorkerState.Halted)
                //.OnEntryFrom(WorkerStateTrigger.NetworkDisconnected, OnHaltDataProcessor, "Worker has network disconnect, halt processor")
                .Permit(WorkerStateTrigger.DataProcessorStart, WorkerState.Running)
                .PermitIf(_checkpointRestoreInitiationTrigger, WorkerState.Recovering, EnsureValidCheckpointId, "Checkpoint validity guard")
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted)
                .OnEntryFrom(WorkerStateTrigger.DataProcessorHaltCompleted, OnHaltDataProcessorCompleted);
                //.Ignore(WorkerStateTrigger.DataProcessorHalt)
                //.Ignore(WorkerStateTrigger.NetworkConnected)
                //.Ignore(WorkerStateTrigger.NetworkDisconnected); //TODO: consider if this trigger is ignorable here or should yield Failed state

            machine.Configure(WorkerState.Running)
                .OnEntry(OnStartDataProcessor)
                //.Permit(WorkerStateTrigger.NetworkDisconnected, WorkerState.Halted)
                .PermitIf(_dataProcessorHaltTrigger, WorkerState.Halting, ((string[], string[]) arg) => true, "")
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted)
                .Ignore(WorkerStateTrigger.DataProcessorStart)
                .Ignore(WorkerStateTrigger.Startup);

            machine.Configure(WorkerState.Recovering)
                .OnEntryFrom(_checkpointRestoreInitiationTrigger, OnStartCheckpointRestore)
                .PermitReentryIf(_checkpointRestoreInitiationTrigger, EnsureValidCheckpointId, "CheckpointId validity guard (reentry)")
                .OnExit(CompleteRestoringCheckpoint)
                .PermitIf(_checkpointRestoreCompletionTrigger, WorkerState.Halted, EnsureCorrectCheckpointRestored, "Expected checkpointId for completion guard")
                .IgnoreIf(_checkpointRestoreCompletionTrigger, (id) => !EnsureCorrectCheckpointRestored(id), "Ignore if checkpointId is not as expected guard")
                .Permit(WorkerStateTrigger.Failure, WorkerState.Faulted);

            machine.Configure(WorkerState.Faulted)
                .Permit(WorkerStateTrigger.Startup, WorkerState.Halted);

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
            return RestoringCheckpointId == checkpointId;
        }

        private bool EnsureValidCheckpointId(Guid checkpointId)
        {
            return checkpointId != default;
        }

        private void OnStartCheckpointRestore(Guid checkpointId)
        {
            RestoringCheckpointId = checkpointId;
            OnStateChangeNotificationRequired?.Invoke(InstanceName, CurrentState);
        }

        private void CompleteRestoringCheckpoint()
        {
            RestoringCheckpointId = default;
            //no notification required
        }

        private void OnStartDataProcessor()
        {
            OnStateChangeNotificationRequired?.Invoke(InstanceName, _stateMachine.State);
        }



        private void OnHaltDataProcessor((string[], string[]) connectedInstancesThatHalt)
        {
            DataProcessorHaltArgs = connectedInstancesThatHalt;
            OnStateChangeNotificationRequired?.Invoke(InstanceName, _stateMachine.State);
        }

        private void OnHaltDataProcessorCompleted()
        {
            DataProcessorHaltArgs = default;
        }
        #endregion

    }
}
