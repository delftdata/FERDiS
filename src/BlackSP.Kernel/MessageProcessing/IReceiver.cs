using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{

    public interface IReceiver<TMessage>
    {

        /// <summary>
        /// Receive a message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task Receive(byte[] message, IEndpointConfiguration origin, int shardId, CancellationToken t);

        /// <summary>
        /// Block incoming messages from specified origin
        /// </summary>
        /// <param name="origin"></param>
        void Block(IEndpointConfiguration origin, int shardId);

        /// <summary>
        /// Unblock specified origin
        /// </summary>
        /// <param name="origin"></param>
        void Unblock(IEndpointConfiguration origin, int shardId);

    }
}
