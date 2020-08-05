using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Controllers
{
    public abstract class MultiSourceProcessControllerBase<TMessage> : IDisposable
        where TMessage : MessageBase
    {
        private readonly IEnumerable<ISource<TMessage>> _sources;
        private readonly IPipeline<TMessage> _pipeline;
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly SemaphoreSlim _csSemaphore;
        private bool disposed;

        public MultiSourceProcessControllerBase(
            IEnumerable<ISource<TMessage>> sources,
            IPipeline<TMessage> pipeline,
            IDispatcher<TMessage> dispatcher)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            if(!_sources.Any())
            {
                throw new ArgumentException($"At least one {typeof(ISource<TMessage>)} expected", nameof(sources));
            }
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _csSemaphore = new SemaphoreSlim(1, 1);

            disposed = false;
        }

        /// <summary>
        /// Start core processes for multi-source message processing
        /// </summary>
        public async Task StartProcess(CancellationToken t)
        {
            var dispatchQueue = new BlockingCollection<TMessage>(1 << 12);//TODO: determine proper capacity
            try
            {
                var threads = StartThreads(dispatchQueue, t);
                await Task.WhenAll(threads).ConfigureAwait(false);
            }
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        private IEnumerable<Task> StartThreads(BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            foreach (var source in _sources)
            {
               yield return Task.Run(() => ProcessFromSource(source, dispatchQueue, t));
            }
            yield return Task.Run(() => DispatchResults(dispatchQueue, t));
        }

        private async Task ProcessFromSource(ISource<TMessage> source, BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    //take a message from the source
                    var message = source.Take(t) ?? throw new Exception($"Received null from {source.GetType()}.Take");
                    //enter the critical section to have the message processed
                    await ProcessMessageInCriticalSection(message, dispatchQueue, t).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
            finally
            {
                dispatchQueue.CompleteAdding();
            }
        }

        private async Task ProcessMessageInCriticalSection(TMessage message, BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                await _csSemaphore.WaitAsync(t).ConfigureAwait(false);
                IEnumerable<TMessage> responses = await _pipeline.Process(message).ConfigureAwait(false);
                foreach (var msg in responses)
                {
                    dispatchQueue.Add(msg, t);
                }
            }
            finally
            {
                _csSemaphore.Release();
            }
        }

        private async Task DispatchResults(BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                _dispatcher.SetFlags(DispatchFlags.Control);
                foreach (var message in dispatchQueue.GetConsumingEnumerable(t))
                {
                    await _dispatcher.Dispatch(message, t).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
            finally
            {
                _dispatcher.SetFlags(DispatchFlags.None);
            }
        }

        #region dispose support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _csSemaphore.Dispose();
                }
                disposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MultiSourceProcessController()
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
