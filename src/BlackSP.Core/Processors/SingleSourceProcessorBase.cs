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
using System.Threading.Tasks;

namespace BlackSP.Core.Processors
{

    public abstract class SingleSourceProcessorBase<TMessage> 
        where TMessage : MessageBase
    {
        private readonly ISource<TMessage> _source;
        private readonly IPipeline<TMessage> _pipeline;
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly ILogger _logger;
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

        /// <summary>
        /// Start core processes for message processing
        /// </summary>
        public virtual async Task StartProcess(CancellationToken t)
        {
            var passthroughQueue = new BlockingCollection<TMessage>(Constants.DefaultThreadBoundaryQueueSize);
            var exitSource = new CancellationTokenSource();
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(t, exitSource.Token);
            try
            {                
                var deliveryThread = Task.Run(() => ProcessFromSource(passthroughQueue, linkedSource.Token));
                var dispatchThread = Task.Run(() => DispatchResults(passthroughQueue, linkedSource.Token));
                var exitedTask = await Task.WhenAny(deliveryThread, dispatchThread).ConfigureAwait(false);
                exitSource.Cancel();
                await Task.WhenAll(deliveryThread, dispatchThread).ConfigureAwait(false);
                await exitedTask.ConfigureAwait(false);
                t.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) {
                /*silence cancellation request exceptions*/
                _logger.Information("Processor exited due to cancellation request");
                throw;
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Processor encountered exception");
                throw;
            }
            finally
            {
                _logger.Information("Processor shutting down");
                passthroughQueue.Dispose();
                exitSource.Dispose();
                linkedSource.Dispose();
            }
        }

        private async Task ProcessFromSource(BlockingCollection<TMessage> passthroughQueue, CancellationToken t) {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    TMessage message;
                    if(_injectedMessage != null)
                    {
                        message = _injectedMessage;
                        _injectedMessage = null;
                    }
                    else
                    {
                        message = await _source.Take(t).ConfigureAwait(false) ?? throw new Exception($"Received null from {_source.GetType()}.Take");
                    }
                    var results = await _pipeline.Process(message).ConfigureAwait(false);
                    foreach (var msg in results)
                    {
                        passthroughQueue.Add(msg, t);
                    }
                }
            }
            finally
            {
                //note: not starting flushing on source as this is requested by a checkpoint restore request
                passthroughQueue.CompleteAdding();
            }
        }

        private async Task DispatchResults(BlockingCollection<TMessage> passthroughQueue, CancellationToken t)
        {
            try
            {
                foreach (var message in passthroughQueue.GetConsumingEnumerable(t))
                {
                    await _dispatcher.Dispatch(message, t).ConfigureAwait(false);
                }
            }
            finally
            {
                //await _dispatcher.Flush().ConfigureAwait(false);
            }
        }

    }

}
