using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface IFlushableQueue<T>
    {
        /// <summary>
        /// Indicates if the queue is currently flushing
        /// </summary>
        bool IsFlushing { get; }

        /// <summary>
        /// Performs a synchronous flush that basically resets the queue
        /// </summary>
        /// <returns></returns>
        Task Flush();

        /// <summary>
        /// Begins flushing the queue
        /// </summary>
        /// <returns></returns>
        Task BeginFlush();

        /// <summary>
        /// Begins flushing the queue but inserts a final target first
        /// </summary>
        /// <returns></returns>
        Task BeginFlush(T target);

        /// <summary>
        /// Ends flushing the queue
        /// </summary>
        /// <returns></returns>
        Task EndFlush();

        void Add(T item, CancellationToken cancellationToken);

        bool TryTake(out T item, int millisecondsTimeout);

        bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken);
    }
}
