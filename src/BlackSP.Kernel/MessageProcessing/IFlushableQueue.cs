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
        /// Throw exception if flushing has started
        /// </summary>
        void ThrowIfFlushingStarted();

        /// <summary>
        /// Begins flushing the queue
        /// </summary>
        /// <returns></returns>
        Task BeginFlush();

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
