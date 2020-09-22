using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.Kernel
{
    public enum ReceptionFlags
    {
        /// <summary>
        /// Flag indicating no message types are forwarded by the receiver
        /// </summary>
        None = 0,
        /// <summary>
        /// Flag indicating control messages are forwarded by the receiver
        /// </summary>
        Control = 1 << 0,
        /// <summary>
        /// Flag indicating data messages are forwarded by the receiver
        /// </summary>
        Data = 1 << 1,
        /// <summary>
        /// Flag indicating message types that are not forwarded are buffered for later delivery
        /// </summary>
        Buffer = 1 << 2
    }

    public interface IReceiver<TMessage>
    {
        /// <summary>
        /// Get a receptionqueue that is associated with this receiver. This queue should only be filled.
        /// References are adviced not to be kept as the collection itself may internally be overwritten.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        BlockingCollection<TMessage> GetReceptionQueue(IEndpointConfiguration origin, int shardId);

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

        /// <summary>
        /// Set the receiver flags
        /// </summary>
        /// <param name="mode"></param>
        void SetFlags(ReceptionFlags mode);

        /// <summary>
        /// Get the receiver flags
        /// </summary>
        /// <returns></returns>
        ReceptionFlags GetFlags();
    }
}
