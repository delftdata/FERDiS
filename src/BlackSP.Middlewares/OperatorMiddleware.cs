using BlackSP.Core;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Middlewares
{
    public class OperatorMiddleware : IMessageMiddleware
    {

        private readonly IOperatorShell _operatorShell;

        public OperatorMiddleware(IOperatorShell operatorShell)
        {
            _operatorShell = operatorShell ?? throw new ArgumentNullException(nameof(operatorShell));
        }

        public IEnumerable<IMessage> Handle(IMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            
            return message.IsControl 
                ? Enumerable.Empty<IMessage>() 
                : _operatorShell.OperateOnEvent(message.Payload).Select(ev => message.Copy(ev));
        }
    }
}
