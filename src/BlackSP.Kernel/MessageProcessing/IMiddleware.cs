using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface IMiddleware<T> where T : IMessage
    {

        Task<IEnumerable<T>> Handle(T message);

    }
}
