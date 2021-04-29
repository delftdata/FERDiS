using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageProcessing.Handlers
{
    public class NoopMessageHandler<TMessage> : IHandler<TMessage> where TMessage : IMessage
    {


        public NoopMessageHandler()
        {
        }

        public Task<IEnumerable<TMessage>> Handle(TMessage message, CancellationToken t)
        {
            return Task.FromResult(new List<TMessage>() { message }.AsEnumerable());
        }
    }
}
