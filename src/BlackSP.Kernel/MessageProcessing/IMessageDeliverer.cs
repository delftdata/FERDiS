using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{
    public interface IMessageDeliverer<T> where T : IMessage
    {
        Task<IEnumerable<T>> Deliver(IMessage message);

    }

    public interface IMessageDeliverer
    {
        Task<IEnumerable<IMessage>> Deliver(IMessage message);
    }

    public interface IControlDeliverer
    {
        Task<IEnumerable<IMessage>> Deliver(IMessage message);
    }
}
