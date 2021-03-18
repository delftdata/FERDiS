using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackSP.Core.Processors
{
    public abstract class MultiSourceProcessorBase<TMessage> : IDisposable
        where TMessage : MessageBase
    {
        private readonly IEnumerable<ISource<TMessage>> _sources;
        private readonly IPipeline<TMessage> _pipeline;
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _csSemaphore;
        private bool disposed;

        public MultiSourceProcessorBase(
            IEnumerable<ISource<TMessage>> sources,
            IPipeline<TMessage> pipeline,
            IDispatcher<TMessage> dispatcher,
            ILogger logger)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            if(!_sources.Any())
            {
                throw new ArgumentException($"At least one {typeof(ISource<TMessage>)} expected", nameof(sources));
            }
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _csSemaphore = new SemaphoreSlim(1, 1);
            disposed = false;
        }

        public virtual Task PreStartHook(CancellationToken t)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Start core processes for multi-source message processing
        /// </summary>
        public async Task StartProcess(CancellationToken t)
        {
            if(!Constants.SkipProcessorPreStartHooks)
            {
                await PreStartHook(t).ConfigureAwait(false);
            }
            var channel = Channel.CreateBounded<TMessage>(new BoundedChannelOptions(Constants.DefaultThreadBoundaryQueueSize) { FullMode = BoundedChannelFullMode.Wait });
            using var exceptionSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, exceptionSource.Token);
            try
            {
                var threads = StartThreads(channel, linkedSource.Token);
                var exitedThread = await Task.WhenAny(threads).ConfigureAwait(false);
                exceptionSource.Cancel();
                await Task.WhenAll(threads).ConfigureAwait(false);
                await exitedThread.ConfigureAwait(false);
            }
            finally
            {
                channel.Writer.Complete();
            }
        }

        private IEnumerable<Task> StartThreads(Channel<TMessage> dispatchChannel, CancellationToken t)
        {
            foreach (var source in _sources)
            {
               yield return Task.Run(() => ProcessFromSource(source, dispatchChannel, t)).ContinueWith(LogException, TaskScheduler.Current);
            }
            yield return Task.Run(() => DispatchResults(dispatchChannel, t)).ContinueWith(LogException, TaskScheduler.Current);
        }

        private void LogException(Task t)
        {
            if(t.IsFaulted)
            {
                _logger.Fatal(t.Exception, "MultiSourceProcessor encountered exception, exiting");
            } 
            else if(t.IsCanceled)
            {
                //shh this is okay
            }
            else
            {
                _logger.Warning($"Thread exited without exception or cancellation");
            }
        }

        private async Task ProcessFromSource(ISource<TMessage> source, Channel<TMessage> dispatchChannel, CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    //take a message from the source
                    var message = await source.Take(t).ConfigureAwait(false) ?? throw new Exception($"Received null from {source.GetType()}.Take");
                    await ProcessMessageInCriticalSection(message, dispatchChannel, t).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
        }

        private async Task ProcessMessageInCriticalSection(TMessage message, Channel<TMessage> dispatchChannel, CancellationToken t)
        {
            try
            {
                await _csSemaphore.WaitAsync(t).ConfigureAwait(false); //enter cs
                IEnumerable<TMessage> responses = await _pipeline.Process(message).ConfigureAwait(false);
                foreach (var msg in responses)
                {
                    if(!await dispatchChannel.Writer.WaitToWriteAsync(t))
                    {
                        throw new InvalidOperationException("Dispatch channel cannot be written to");
                    }
                    await dispatchChannel.Writer.WriteAsync(msg, t);
                }
            }
            finally
            {
                _csSemaphore.Release(); //leave cs
            }
        }

        private async Task DispatchResults(Channel<TMessage> dispatchChannel, CancellationToken t)
        {
            try
            {
                while (await dispatchChannel.Reader.WaitToReadAsync(t))
                {
                    var message = await dispatchChannel.Reader.ReadAsync(t);
                    await _dispatcher.Dispatch(message, t).ConfigureAwait(false);
                }
            }
            finally
            {
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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
