using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.Kernel
{

    public interface IReceiver<TMessage>
    {
        /// <summary>
        /// Get a receptionqueue that is associated with this receiver. This queue should only be filled.
        /// References are adviced not to be kept as the collection itself may internally be overwritten.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        IFlushableQueue<TMessage> GetReceptionQueue(IEndpointConfiguration origin, int shardId);

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
