using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface IFlushable
    {
        /// <summary>
        /// Throw exception if flushing has started
        /// </summary>
        void ThrowIfFlushingStarted();

        /// <summary>
        /// Begins flushing
        /// </summary>
        /// <returns></returns>
        Task BeginFlush();

        /// <summary>
        /// Ends flushing
        /// </summary>
        /// <returns></returns>
        Task EndFlush();
    }

    public interface IFlushable<T> : IFlushable
    {
        T UnderlyingCollection { get; }

    }

    [Obsolete]
    public interface IFlushableQueue<T> : IFlushable
    {

        void Add(T item, CancellationToken cancellationToken);

        bool TryTake(out T item, int millisecondsTimeout);

        bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken);
    }
}
