using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{
    public interface IPipeline<T> where T : IMessage
    {
        Task<IEnumerable<T>> Process(T message);

    }
}
