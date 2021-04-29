using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface IHandler<T>
    {

        Task<IEnumerable<T>> Handle(T message, CancellationToken t);

    }
}
