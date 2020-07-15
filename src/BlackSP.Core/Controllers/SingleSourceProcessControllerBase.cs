using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Controllers
{

    public abstract class SingleSourceProcessControllerBase<TMessage> 
        where TMessage : MessageBase
    {
        private readonly ISource<TMessage> _source;
        private readonly IPipeline<TMessage> _pipeline;
        private readonly IDispatcher<TMessage> _dispatcher;

        public SingleSourceProcessControllerBase(
            ISource<TMessage> source,
            IPipeline<TMessage> pipeline,
            IDispatcher<TMessage> dispatcher)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            
        }

        /// <summary>
        /// Start core processes for message processing
        /// </summary>
        public async Task StartProcess(CancellationToken t)
        {
            var dispatchQueue = new BlockingCollection<TMessage>(1 << 14);//TODO: determine proper capacity
            try
            {
                var deliveryThread = Task.Run(() => ProcessFromSource(dispatchQueue, t));
                var dispatchThread = Task.Run(() => DispatchResults(dispatchQueue, t));
                await Task.WhenAll(deliveryThread, dispatchThread).ConfigureAwait(false);
                t.ThrowIfCancellationRequested();
            } 
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        private async Task ProcessFromSource(BlockingCollection<TMessage> dispatchQueue, CancellationToken t) {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    var message = _source.Take(t) ?? throw new Exception($"Received null from {_source.GetType()}.Take");
                    var results = await _pipeline.Process(message).ConfigureAwait(false);
                    foreach (var msg in results)
                    {
                        dispatchQueue.Add(msg, t);
                    }
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ } 
            finally
            {
                dispatchQueue.CompleteAdding();
            }
        }

        private async Task DispatchResults(BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                _dispatcher.SetFlags(DispatchFlags.Control | DispatchFlags.Data);
                foreach (var message in dispatchQueue.GetConsumingEnumerable(t))
                {
                    await _dispatcher.Dispatch(message, t).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
        }

    }

}
