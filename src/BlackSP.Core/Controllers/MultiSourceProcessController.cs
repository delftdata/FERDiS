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
        private readonly IEnumerable<IMessageSource<TMessage>> _controlSources;
        private readonly IMessageDeliverer<TMessage> _deliverer;
        private readonly IDispatcher<TMessage> _dispatcher;
        private readonly SemaphoreSlim _delivererSemaphore;


        private CancellationTokenSource _ctSource;
        private Task _activeProcess;

        public MultiSourceProcessController(
            IEnumerable<IMessageSource<TMessage>> controlSources,
            IMessageDeliverer<TMessage> messageDeliverer,
            IDispatcher<TMessage> dispatcher)
        {
            _controlSources = controlSources ?? throw new ArgumentNullException(nameof(controlSources));
            if(!_controlSources.Any())
            {
                throw new ArgumentException("At least one IMessageSource expected", nameof(controlSources));
            }

            _deliverer = messageDeliverer ?? throw new ArgumentNullException(nameof(messageDeliverer));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _delivererSemaphore = new SemaphoreSlim(1, 1);

            _ctSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Start core processes for multi-source message processing
        /// </summary>
        public async Task StartProcess()
        {
            var t = _ctSource.Token;
            var dispatchQueue = new BlockingCollection<TMessage>(64);//TODO: determine proper capacity
            try
            {
                var threads = new List<Task>();
                foreach(var controlSource in _controlSources)
                {
                    threads.Add(Task.Run(async () => await DeliverFromSource(controlSource, dispatchQueue, t).ConfigureAwait(false)));
                }
                threads.Add(Task.Run(async () => await DispatchResults(dispatchQueue, t).ConfigureAwait(false)));

                _activeProcess = Task.WhenAll(threads);
                await _activeProcess.ConfigureAwait(false);
            }
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        public async Task StopProcess()
        {
            //TODO: move to dispose and tear down system
            _ctSource.Cancel();
            try
            {
                await (_activeProcess ?? Task.CompletedTask).ConfigureAwait(false);
            } catch { } //silence any exception
            
            _ctSource = new CancellationTokenSource();
            
        }

        private async Task DeliverFromSource(IMessageSource<TMessage> controlSource, BlockingCollection<TMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    var message = controlSource.Take(t) ?? throw new Exception($"Received null from {controlSource.GetType()}.Take");
                    await _delivererSemaphore.WaitAsync(t).ConfigureAwait(true);
                    IEnumerable<TMessage> responses = await _deliverer.Deliver(message).ConfigureAwait(false);
                    foreach (var msg in responses)
                    {
                        dispatchQueue.Add(msg);
                    }
                    _delivererSemaphore.Release();
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
            catch
            {
                _ctSource.Cancel();
                throw;
            }
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
            catch
            {
                _ctSource.Cancel();
                throw;
            }
            finally
            {
                _dispatcher.SetFlags(DispatchFlags.None);
            }
        }

    }
}
