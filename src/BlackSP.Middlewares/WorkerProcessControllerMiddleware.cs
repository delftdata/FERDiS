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
    public class PassthroughMiddleware : IMiddleware<DataMessage>
    {


        public PassthroughMiddleware()
        {
        }

        public Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            return Task.FromResult(new List<DataMessage>() { message }.AsEnumerable());
        }
    }
}
