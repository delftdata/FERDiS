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

    /// <summary>
    /// Core element responsible for dispatching messages to their respective output channels<br/>
    /// Responsible for serialization and optionally partitioning
    /// </summary>
    public interface IDispatcher<T>
    {
        /// <summary>
        /// Dispatches provided targets
        /// </summary>
        /// <param name="target"></param>
        Task Dispatch(T target, CancellationToken t);

        /// <summary>
        /// Returns an (endpoint + shard) unique queue of ready-to-egress bytes
        /// </summary>
        /// <param name="endpointName"></param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        IFlushableQueue<byte[]> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId);

        /// <summary>
        /// Begins flushing process in the dispatcher
        /// </summary>
        /// <returns></returns>
        Task BeginFlush();

        /// <summary>
        /// Ends flushing process in the dispatcher
        /// </summary>
        /// <returns></returns>
        Task EndFlush();
    }
}
