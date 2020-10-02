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
        private readonly SemaphoreSlim _flushing;
        private TaskCompletionSource<bool> _tcs;

        public bool IsFlushing => _tcs != null;
        public BlockingCollection<T> UnderlyingCollection => _queue;

        public BlockingFlushableQueue(int boundedCapacity)
        {
            _capacity = boundedCapacity;
            _queue = new BlockingCollection<T>(_capacity);
            _flushing = new SemaphoreSlim(1, 1);
        }

        public void Add(T item, CancellationToken cancellationToken)
        {
            if(_queue.IsAddingCompleted)
            {   //if adding is completed opaguely discard messages
                throw new InvalidOperationException("Cannot add to queue, flush in progress");
            }
            _queue.Add(item, cancellationToken);
        }

        public Task Flush()
        {
            try
            {
                _flushing.Wait();
                _queue.CompleteAdding();
                _queue.Dispose();
                _queue = new BlockingCollection<T>(_capacity);
            }
            finally
            {
                _flushing.Release();
            }
            return Task.CompletedTask;
        }

        public Task BeginFlush()
        {
            try
            {
                _flushing.Wait();
                if (_tcs == null)
                {
                    _queue.CompleteAdding();
                    _tcs = new TaskCompletionSource<bool>();
                }
                return _tcs.Task;
            }
            finally
            {
                _flushing.Release();
            }
        }

        public Task BeginFlush(T target)
        {
            try
            {
                _flushing.Wait();
                if (_queue.IsAddingCompleted)
                {
                    throw new InvalidOperationException("Cannot insert message on already flushing queue");
                }
                if(_tcs == null)
                {
                    //_queue.CompleteAdding();
                    _queue.Dispose();
                    _queue = new BlockingCollection<T>(_capacity);
                    _queue.Add(target);
                    _queue.CompleteAdding();
                    _tcs = new TaskCompletionSource<bool>();
                }
                return _tcs.Task;
            }
            finally
            {
                _flushing.Release();
            }
        }

        public Task EndFlush()
        {            
            _ = _tcs ?? throw new InvalidOperationException("Cannot end flush that was not started");
            try
            {
                _flushing.Wait();
                _queue.Dispose();
                _queue = new BlockingCollection<T>(_capacity);
                _tcs.SetResult(true);
                _tcs = null;
            } 
            finally
            {
                _flushing.Release();
            }
            return Task.CompletedTask;
        }

        public bool TryTake(out T item, int millisecondsTimeout)
        {
            if (!_flushing.Wait(millisecondsTimeout))
            {
                item = default;
                return false;
            }
            try
            {
                return _queue.TryTake(out item, millisecondsTimeout);
            }
            finally
            {
                _flushing.Release();
            }
        }

        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if(!_flushing.Wait(millisecondsTimeout, cancellationToken))
            {
                item = default;
                return false;
            }

            try
            {
                return _queue.TryTake(out item, millisecondsTimeout, cancellationToken);
            }
            finally
            {
                _flushing.Release();
            }
        }

        #region dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _queue.Dispose();
                    _flushing.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BlockingDispatchQueue()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
