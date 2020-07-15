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

    public class SingleSourceProcessController<TMessage> 
        where TMessage : MessageBase
    {
        private readonly ISource<TMessage> _dataSource;
        private readonly IPipeline<TMessage> _deliverer;
        private readonly IDispatcher<TMessage> _dispatcher;

        public SingleSourceProcessController(
            ISource<TMessage> dataSource,
            IPipeline<TMessage> dataDeliverer,
            IDispatcher<TMessage> dispatcher)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _deliverer = dataDeliverer ?? throw new ArgumentNullException(nameof(dataDeliverer));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            
        }

        /// <summary>
        /// Start core processes for data and control message processing
        /// </summary>
        public async Task StartProcess(CancellationToken t)
        {
            var dispatchQueue = new BlockingCollection<TMessage>(64);//TODO: determine proper capacity
            try
            {
                var deliveryThread = Task.Run(() => DeliverFromSource(dispatchQueue, t));
                var dispatchThread = Task.Run(() => DispatchResults(dispatchQueue, t));
                await Task.WhenAll(deliveryThread, dispatchThread).ConfigureAwait(false);
                t.ThrowIfCancellationRequested();
            } 
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        private async Task DeliverFromSource(BlockingCollection<TMessage> dispatchQueue, CancellationToken t) {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    var message = _dataSource.Take(t) ?? throw new Exception($"Received null from {_dataSource.GetType()}.Take");
                    var results = await _deliverer.Deliver(message).ConfigureAwait(false);
                    foreach (var msg in results)
                    {
                        dispatchQueue.Add(msg);
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
