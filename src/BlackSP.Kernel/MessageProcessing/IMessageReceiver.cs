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

    public interface IMessageReceiver
    {
        /// <summary>
        /// Returns a blocking enumerable containing messages that are eligible to be delivered (control & data messages)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        IEnumerable<IMessage> GetReceivedMessageEnumerator(CancellationToken t);

        /// <summary>
        /// Drop a new message in the receiver
        /// </summary>
        /// <param name="message"></param>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        void Receive(IMessage message, IEndpointConfiguration origin, int shardId);

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
