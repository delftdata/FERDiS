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
        /// Throws implementation specific eceptions indicating unmet preconditions for reception
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        void ThrowIfReceivePreconditionsNotMet(IEndpointConfiguration origin, int shardId);

        /// <summary>
        /// Block incoming messages from specified origin
        /// </summary>
        /// <param name="origin"></param>
        Task Block(IEndpointConfiguration origin, int shardId);

        /// <summary>
        /// Unblock specified origin
        /// </summary>
        /// <param name="origin"></param>
        void Unblock(IEndpointConfiguration origin, int shardId);

       
        /// <summary>
        /// Gain exclusive access to the receiver, during which other connections will asynchronously wait for priority to be released.<br/>
        /// Primary use is for processing backchannel messages which must be prioritized to ensure enough buffer capacity.
        /// </summary>
        /// <param name="prioOrigin"></param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        Task TakePriority(IEndpointConfiguration prioOrigin, int shardId);

        /// <summary>
        /// Release exclusive access to the receiver. Ensure to not call this method only after taking priority.
        /// </summary>
        /// <param name="prioOrigin"></param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        void ReleasePriority(IEndpointConfiguration prioOrigin, int shardId);
    }
}
