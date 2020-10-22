using BlackSP.Core.Exceptions;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Models
{
    public class BlockingFlushableQueue<T> : IFlushableQueue<T>, IDisposable
    {
        private readonly int _capacity;
        private BlockingCollection<T> _queue;
        private bool disposedValue;
        private volatile TaskCompletionSource<bool> _tcs;
        
        private bool IsFlushing => _tcs != null;
        
        public BlockingCollection<T> UnderlyingCollection => _queue;

        public BlockingFlushableQueue(int boundedCapacity)
        {
            _capacity = boundedCapacity;
            _queue = new BlockingCollection<T>(_capacity);
        }

        public void ThrowIfFlushingStarted()
        {
            if(IsFlushing)
            {
                throw new FlushInProgressException();
            }
        }

        public void Add(T item, CancellationToken cancellationToken)
        {   
            try
            {
                ThrowIfFlushingStarted();
                _queue?.Add(item, cancellationToken);
            }
            catch(InvalidOperationException)
            {
                if(!_queue?.IsAddingCompleted ?? false)
                {
                    throw; //silence failed additions due to adding completion
                }
                ThrowIfFlushingStarted();
            }
        }

        public bool TryTake(out T item, int millisecondsTimeout)
        {
            if (IsFlushing)
            {
                item = default;
                return false;
            }
            return _queue.TryTake(out item, millisecondsTimeout);
        }

        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (IsFlushing)
            {
                Task.Delay(millisecondsTimeout).Wait();
                item = default;
                return false;
            }
            return _queue.TryTake(out item, millisecondsTimeout, cancellationToken);
        }

        public async Task BeginFlush()
        {
            if (_tcs == null)
            {                    
                _queue.CompleteAdding();
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            await _tcs.Task;
            //_tcs = null;
        }

        public async Task EndFlush()
        {
            while(_tcs == null)
            {
                await Task.Delay(100).ConfigureAwait(false); //spin until flush started
            }
            _queue.Dispose();
            _queue = new BlockingCollection<T>(_capacity);
            _tcs.SetResult(true);
            _tcs = null;
        }

        #region dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _queue.Dispose();
                    //_flushing.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
