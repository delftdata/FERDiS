using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageProcessing.Processors
{

    public abstract class SingleSourceProcessorBase<TMessage> 
        where TMessage : MessageBase
    {
        private readonly ISource<TMessage> _source;
        private readonly IPipeline<TMessage> _pipeline;
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly ILogger _logger;
        
        private SemaphoreSlim _pauseSemaphore;
        private CancellationTokenSource _processTokenSource;
        private Task _processThread;
        private TMessage _injectedMessage;

        public SingleSourceProcessorBase(
            ISource<TMessage> source,
            IPipeline<TMessage> pipeline,
            IDispatcher<TMessage> dispatcher,
            ILogger logger)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _pauseSemaphore = new SemaphoreSlim(1, 1);

        }

        /// <summary>
        /// Inject a new message that will be processed next, before taking any more messages from the underlying source
        /// </summary>
        /// <param name="message"></param>
        public void Inject(TMessage message)
        {
            if(_injectedMessage != null)
            {
                throw new InvalidOperationException("Cannot inject a message while another one is already injected");
            }
            _injectedMessage = message ?? throw new ArgumentNullException(nameof(message));
        }

        public virtual Task PreStartHook(CancellationToken t)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Start core processes for message processing
        /// </summary>
        public async Task StartProcess(CancellationToken t)
        {
            if (!Constants.SkipProcessorPreStartHooks)
            {
                await PreStartHook(t).ConfigureAwait(false);
            }
            var channel = Channel.CreateBounded<TMessage>(new BoundedChannelOptions(Constants.DefaultThreadBoundaryQueueSize) { FullMode = BoundedChannelFullMode.Wait });
            _pauseSemaphore = new SemaphoreSlim(1, 1);
            _processTokenSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, _processTokenSource.Token);
            try
            {                
                var deliveryThread = Task.Run(() => ProcessFromSource(channel, linkedSource.Token));
                var dispatchThread = Task.Run(() => DispatchResults(channel, linkedSource.Token));
                _processThread = Task.WhenAll(deliveryThread, dispatchThread);
                var exitedTask = await Task.WhenAny(deliveryThread, dispatchThread).ConfigureAwait(false);
                _processTokenSource.Cancel();
                await _processThread.ConfigureAwait(false);
                await exitedTask.ConfigureAwait(false);
                t.ThrowIfCancellationRequested();
                _logger.Information("Processor exited gracefully");
            }
            catch (OperationCanceledException) {
                /*silence cancellation request exceptions*/
                _logger.Debug("Processor exited due to cancellation request");
                throw;
            }
            catch (Exception e)
            {
                _logger.Warning(e, "Processor exited due to exception");
                throw;
            } 
            finally
            {
                _processTokenSource.Dispose();
                _processTokenSource = null;
            }
        }

        public async Task StopProcess()
        {
            if(_processThread == null || _processTokenSource == null)
            {
                return;
            }
            _processTokenSource.Cancel();
            await _processThread.ConfigureAwait(false);
        }

        public async Task Pause()
        {
            await _pauseSemaphore.WaitAsync().ConfigureAwait(false);
        }

        public void Unpause()
        {
            _pauseSemaphore.Release();
        }

        private async Task ProcessFromSource(Channel<TMessage> dispatchChannel, CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    TMessage message;
                    if (_injectedMessage != null)
                    {
                        message = _injectedMessage;
                        _injectedMessage = null;
                    }
                    else
                    {
                        message = await _source.Take(t).ConfigureAwait(false) ?? throw new Exception($"Received null from {_source.GetType()}.Take");
                    }

                    await _pauseSemaphore.WaitAsync(t).ConfigureAwait(false);
                    try
                    {
                        foreach (var output in await _pipeline.Process(message, t).ConfigureAwait(false))
                        {
                            await dispatchChannel.Writer.WriteAsync(output, t).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        _pauseSemaphore.Release();
                    }
                }
            }
            finally
            {
                //note: not starting flushing on source as this is requested by a checkpoint restore request
                dispatchChannel.Writer.Complete();
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

    }

}
