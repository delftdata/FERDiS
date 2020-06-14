using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Middlewares
{
    public class OperatorMiddleware : IMiddleware<DataMessage>
    {

        private readonly IOperatorShell _operatorShell;

        public OperatorMiddleware(IOperatorShell operatorShell)
        {
            _operatorShell = operatorShell ?? throw new ArgumentNullException(nameof(operatorShell));
        }

        public Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            
            var result = message.IsControl 
                ? Enumerable.Empty<DataMessage>() 
                : _operatorShell.OperateOnEvent(message.Payload).Select(ev => message.Copy(ev));

            return Task.FromResult(result);
        }
    }
}
