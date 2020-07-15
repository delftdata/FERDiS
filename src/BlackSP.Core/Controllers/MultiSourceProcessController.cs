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
    public class MultiSourceProcessController<TMessage>
        where TMessage : MessageBase
    {
        private readonly IEnumerable<ISource<TMessage>> _controlSources;
        private readonly IPipeline<TMessage> _pipeline;
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly SemaphoreSlim _delivererSemaphore;

        public MultiSourceProcessController(
            IEnumerable<ISource<TMessage>> controlSources,
            IPipeline<TMessage> messagePipeline,
            IDispatcher<TMessage> dispatcher)
        {
            _controlSources = controlSources ?? throw new ArgumentNullException(nameof(controlSources));
            if(!_controlSources.Any())
            {
                throw new ArgumentException("At least one IMessageSource expected", nameof(controlSources));
            }

            _pipeline = messagePipeline ?? throw new ArgumentNullException(nameof(messagePipeline));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _delivererSemaphore = new SemaphoreSlim(1, 1);

        }

        /// <summary>
        /// Start core processes for multi-source message processing
        /// </summary>
        public async Task StartProcess(CancellationToken t)
        {
            var dispatchQueue = new BlockingCollection<TMessage>(64);//TODO: determine proper capacity
            try
            {
                var threads = new List<Task>();
                foreach(var controlSource in _controlSources)
                {
                    threads.Add(Task.Run(async () => await DeliverFromSource(controlSource, dispatchQueue, t).ConfigureAwait(false)));
                }
                threads.Add(Task.Run(async () => await DispatchResults(dispatchQueue, t).ConfigureAwait(false)));

                await Task.WhenAll(threads).ConfigureAwait(false);
            }
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        private async Task DeliverFromSource(ISource<TMessage> controlSource, BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    var message = controlSource.Take(t) ?? throw new Exception($"Received null from {controlSource.GetType()}.Take");
                    await _delivererSemaphore.WaitAsync(t).ConfigureAwait(false);
                    IEnumerable<TMessage> responses = await _pipeline.Deliver(message).ConfigureAwait(false);
                    foreach (var msg in responses)
                    {
                        dispatchQueue.Add(msg);
                    }
                    _delivererSemaphore.Release();
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

    }
}
