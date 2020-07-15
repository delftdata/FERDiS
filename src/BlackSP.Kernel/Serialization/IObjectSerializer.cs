using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Serialization
{
    public interface IObjectSerializer<T>
    {
        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<byte[]> SerializeAsync(T message, CancellationToken t);

        /// <summary>
        /// </summary>
        /// <param name="msgBytes"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<T> DeserializeAsync(byte[] msgBytes, CancellationToken t);
    }
}
