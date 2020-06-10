using BlackSP.Core;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Middlewares
{
    public class OperatorMiddleware : IMiddleware
    {

        private readonly IOperatorShell _operatorShell;

        public OperatorMiddleware(IOperatorShell operatorShell)
        {
            _operatorShell = operatorShell ?? throw new ArgumentNullException(nameof(operatorShell));
        }

        public Task<IEnumerable<IMessage>> Handle(IMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            
            var result = message.IsControl 
                ? Enumerable.Empty<IMessage>() 
                : _operatorShell.OperateOnEvent(message.Payload).Select(ev => message.Copy(ev));

            return Task.FromResult(result);
        }
    }
}
