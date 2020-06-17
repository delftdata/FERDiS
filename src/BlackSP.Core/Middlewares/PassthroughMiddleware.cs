using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class PassthroughMiddleware<TMessage> : IMiddleware<TMessage> where TMessage : IMessage
    {


        public PassthroughMiddleware()
        {
        }

        public Task<IEnumerable<TMessage>> Handle(TMessage message)
        {
            return Task.FromResult(new List<TMessage>() { message }.AsEnumerable());
        }
    }
}
