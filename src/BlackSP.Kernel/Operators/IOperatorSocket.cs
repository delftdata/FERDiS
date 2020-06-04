using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
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

        IEnumerable<IEvent> OperateOnEvent(IEvent @event);

    }
}
