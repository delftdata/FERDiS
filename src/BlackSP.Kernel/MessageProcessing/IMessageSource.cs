using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface IMessageSource<T> where T : IMessage
    {
        /// <summary>
        /// Flush the underlying message source
        /// </summary>
        /// <returns></returns>
        Task Flush();

        /// <summary>
        /// Take the next message from the message source
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        T Take(CancellationToken t);
    }
}
