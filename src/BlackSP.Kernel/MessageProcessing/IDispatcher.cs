﻿using BlackSP.Kernel.Endpoints;
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
        /// Flag indicating wether data messages are expected to be dispatched
        /// </summary>
        Data = 1 << 0,
        /// <summary>
        /// Flag indicating wether control messages are expected to be dispatched
        /// </summary>
        Control = 1 << 1,
        /// <summary>
        /// Flag indicating wether non-dispatched message types are expected to be buffered for later dispatching
        /// </summary>
        Buffer = 1 << 2,
    }

    /// <summary>
    /// Core element responsible for dispatching messages to their respective output channels<br/>
    /// Responsible for serialization and partitioning
    /// </summary>
    public interface IDispatcher<T> where T : IMessage
    {
        /// <summary>
        /// Dispatches provided messages
        /// </summary>
        /// <param name="message"></param>
        Task Dispatch(T message, CancellationToken t);

        /// <summary>
        /// Returns an (endpoint + shard) unique queue of ready-to-egress bytes
        /// </summary>
        /// <param name="endpointName"></param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        BlockingCollection<byte[]> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId);

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