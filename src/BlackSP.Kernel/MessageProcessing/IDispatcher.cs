using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
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
        IFlushable<Channel<byte[]>> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId);

        /// <summary>
        /// Empty all dispatch queues
        /// </summary>
        /// <returns></returns>
        Task Flush(IEnumerable<string> downstreamInstancesToFlush);
    }
}
