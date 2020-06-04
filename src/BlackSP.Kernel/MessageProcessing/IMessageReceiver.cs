using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.Kernel
{
    public enum ReceiverMode
    {
        /// <summary>
        /// Flag indicating no message types are accepted by the receiver
        /// </summary>
        None = 0 << 0,
        /// <summary>
        /// Flag indicating control messages are accepted by the receiver
        /// </summary>
        Control = 1 << 0,
        /// <summary>
        /// Flag indicating data messages are accepted by the receiver
        /// </summary>
        Data = 1 << 1,
        /// <summary>
        /// Joint flag indicating all message types are accepted by the receiver
        /// </summary>
        All = Control | Data
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
        /// Change the receiver's internal mode, decides which messages types to store internally and which to provide to the output enumerator
        /// </summary>
        /// <param name="mode"></param>
        void SetMode(ReceiverMode mode);
    }
}
