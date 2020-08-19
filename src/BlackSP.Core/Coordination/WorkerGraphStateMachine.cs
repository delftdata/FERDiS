using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Coordination
{
    public enum WorkerGraphState
    {
        Starting,
        Faulted,
        Recovering
    }

    public class WorkerGraphStateMachine
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <param name="workerInstanceNames"></param>
        /// <returns></returns>
        public delegate WorkerGraphStateMachine Factory(IEnumerable<string> workerInstanceNames);

        private readonly IDictionary<string, WorkerStateMachine> _workerStateMachines;

        public delegate void GraphStateChangeEventArgs();

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

        private void WorkerStateMachine_OnStateChange(string affectedInstanceName, WorkerStateMachine.WorkerState oldState, WorkerStateMachine.WorkerState newState)
        {
            throw new NotImplementedException();
        }

        

    }
}
