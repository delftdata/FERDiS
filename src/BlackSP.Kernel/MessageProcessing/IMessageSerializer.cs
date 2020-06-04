using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{
    public interface IMessageSerializer
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<byte[]> SerializeMessage(IMessage message, CancellationToken t);

        /// <summary>
        /// </summary>
        /// <param name="msgBytes"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<IMessage> DeserializeMessage(byte[] msgBytes, CancellationToken t);
    }
}
