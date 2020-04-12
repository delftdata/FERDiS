using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class OperatorShellHost
    {

        private readonly IOperatorSocket _operator;

        public OperatorShellHost(IOperatorSocket @operator)
        {
            _operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
        }

        public async Task Start(string instanceName)
        {
            Console.WriteLine($"{instanceName} - Starting operator socket");
            await _operator.Start(DateTime.Now);
        }
    }
}
