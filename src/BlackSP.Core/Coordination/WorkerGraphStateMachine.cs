using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Coordination
{
    
    public class WorkerGraphStateMachine
    {
        public enum State
        {
            Idle,
            Running,
            /// <summary>
            /// State indicating one or more workers are in faulted state (reentrant in face of multiple failures)
            /// </summary>
            Faulted,
            Recovering
        }

        public enum Trigger
        {
            /// <summary>
            /// Trigger indicating that one or more workers have reached a faulted state
            /// </summary>
            WorkersFaulted,
            /// <summary>
            /// Trigger indicating that all workers have reached running or halted state
            /// </summary>
            WorkersHealthy
        }

        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="workerInstanceNames"></param>
        /// <returns></returns>
        public delegate WorkerGraphStateMachine Factory(IEnumerable<string> workerInstanceNames);

        private readonly IDictionary<string, WorkerStateMachine> _workerStateMachines;

        public delegate void GraphStateChangeEventArgs();
        public event GraphStateChangeEventArgs Lol;

        public WorkerGraphStateMachine(IEnumerable<string> workerInstanceNames, WorkerStateMachine.Factory stateMachineFactory)
        {
            _ = workerInstanceNames ?? throw new ArgumentNullException(nameof(workerInstanceNames));
            _ = stateMachineFactory ?? throw new ArgumentNullException(nameof(stateMachineFactory));
            _workerStateMachines = new Dictionary<string, WorkerStateMachine>();

            foreach(var instanceName in workerInstanceNames)
            {
                var workerStateMachine = stateMachineFactory.Invoke(instanceName);
                _workerStateMachines.Add(instanceName, workerStateMachine);

                workerStateMachine.OnStateChange += WorkerStateMachine_OnStateChange;
            }
        }

        private void WorkerStateMachine_OnStateChange(string affectedInstanceName, WorkerStateMachine.State newState)
        {
            //if any faulted (even when already other faulted)
            //- prepare recovery line (or overwrite if already present)
            //- halt workers that are affected (and in running state)
            //- wait
           
            //... workers will automagically go back to halted state when they restart

            //when no faulted and a subset is halted..
            //if recovery line prepared, transition to restoring state
            //if no recovery line prepared, transition to running

            //when restoring
            //- trigger restore in every worker state machine with checkpoint ID
            //- remove prepared recovery line

            //... workers will automagically go back to halted state when checkpoint restore completes
        }

        

    }
}
