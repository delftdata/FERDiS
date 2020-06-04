using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{
    public interface IMessageDeliverer
    {
        //Task<ICollection<IMessage>> Deliver(IMessage message, CancellationToken t);

        Task Deliver(IEnumerable<IMessage> messages, CancellationToken t);

        /// <summary>
        /// Returns enumerator containing messages resulting from previous deliveries
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        IEnumerable<IMessage> GetDeliveryResultEnumerator(CancellationToken t);
    }
}
