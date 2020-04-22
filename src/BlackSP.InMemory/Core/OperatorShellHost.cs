using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class OperatorShellHost
    {

        private readonly IOperatorShell _operator;

        public OperatorShellHost(IOperatorShell @operator)
        {
            _operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
        }

        public Task Start(string instanceName)
        {
            Console.WriteLine($"{instanceName} - Starting operator shell {_operator.GetType()}");
            return _operator.Start(DateTime.Now);
        }
    }
}
