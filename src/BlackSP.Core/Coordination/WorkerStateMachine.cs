using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Coordination
{
    public class WorkerStateMachine
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="workerInstanceName"></param>
        /// <returns></returns>
        public delegate WorkerStateMachine Factory(string workerInstanceName);

        public string InstanceName { get; private set; }
        public WorkerState CurrentState { get; private set; }

        public event WorkerStateChangeEvent OnStateChange;
        public delegate void WorkerStateChangeEvent(string affectedInstanceName, WorkerState oldState, WorkerState newState);


        public enum WorkerState
        {
            Offline,
            Launchable,
            Launched,
            Halted,
            Restoring,
            Faulted
        }

        public WorkerStateMachine(string workerInstanceName)
        {
            InstanceName = workerInstanceName ?? throw new ArgumentNullException(nameof(workerInstanceName));
        }

        /// <summary>
        /// Notify the statemachine of a failure in the worker
        /// </summary>
        public void NotifyFailure()
        {

        }

        /// <summary>
        /// Notify the statemachine that a connection change has occurred in the worker
        /// </summary>
        public void NotifyConnectionChange()
        {

        }

        /// <summary>
        /// Notify the statemachine that the worker started working
        /// </summary>
        public void NotifyDataProcessorStart()
        {

        }

        /// <summary>
        /// Notify the statemachine that the worker was halted
        /// </summary>
        public void NotifyDataProcessorHalt()
        {
            //when halted the worker is ready to restore a checkpoint
        }

        /// <summary>
        /// Notify the statemachine that the worker started restoring a checkpoint
        /// </summary>
        public void NotifyCheckpointRestoreStart()
        {
            //if already restoring.. send another request but keep in mind the original response will come first
            //if already restoring.. check if new request is not same checkpoint, if so we need not change any internal state here..
        }

        /// <summary>
        /// Notify the statemachine that the worker completed restoring a checkpoint
        /// </summary>
        public void NotifyCheckpointRestoreCompletion()
        {

        }

    }
}
