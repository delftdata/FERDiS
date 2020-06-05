using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{

    public enum DispatchFlags
    {
        None = 0,
        /// <summary>
        /// Flag indicating wether data messages are expected to be delivered
        /// </summary>
        Data = 1 << 0,
        /// <summary>
        /// Flag indicating wether control messages are expected to be delivered
        /// </summary>
        Control = 1 << 1,
        /// <summary>
        /// Flag indicating wether non-delivered message types are expected to be buffered for later delivery
        /// </summary>
        Buffer = 1 << 2,
    }

    /// <summary>
    /// Core element responsible for dispatching messages to their respective output channels<br/>
    /// Responsible for serialization and partitioning
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Dispatches provided messages
        /// </summary>
        /// <param name="message"></param>
        Task Dispatch(IEnumerable<IMessage> messages, CancellationToken t);

        /// <summary>
        /// Returns an (endpoint + shard) unique queue of ready-to-egress bytes
        /// </summary>
        /// <param name="endpointName"></param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        BlockingCollection<byte[]> GetDispatchQueue(string endpointName, int shardId);

        /// <summary>
        /// Get the dispatcher flags
        /// </summary>
        /// <returns></returns>
        DispatchFlags GetFlags();

        /// <summary>
        /// Set the dispatcher flags
        /// </summary>
        /// <param name="flags"></param>
        void SetFlags(DispatchFlags flags);
    }
}
