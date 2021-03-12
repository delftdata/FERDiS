using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Operators
{
    public interface IOperatorShell
    {

        Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event, bool isCycleInput = false);

    }
}
