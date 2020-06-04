using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel
{
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
    }
}
