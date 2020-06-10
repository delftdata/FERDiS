using BlackSP.Core.Extensions;
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

namespace BlackSP.Core.ProcessManagers
{
    public class ControlProcessController : IProcessManager
    {
        private readonly IEnumerable<IMessageSource<ControlMessage>> _controlSources; //implementations are receiver & heartbeat generator
        
        private readonly IControlDeliverer _deliverer;
        private readonly IDispatcher _dispatcher;

        private CancellationTokenSource _ctSource;
        private Task _activeProcess;

        public ControlProcessController(
            IEnumerable<IMessageSource<ControlMessage>> controlSources,
            IControlDeliverer messageDeliverer,
            IDispatcher dispatcher)
        {
            _controlSources = controlSources ?? throw new ArgumentNullException(nameof(controlSources));
            if(!_controlSources.Any())
            {
                throw new ArgumentException("At least one IControlSource expected", nameof(controlSources));
            }

            _deliverer = messageDeliverer ?? throw new ArgumentNullException(nameof(messageDeliverer));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _ctSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Start core processes for control message processing
        /// </summary>
        public async Task StartProcess()
        {
            var t = _ctSource.Token;
            var dispatchQueue = new BlockingCollection<IMessage>(64);//TODO: determine proper capacity
            try
            {
                var threads = new List<Task>();
                foreach(var controlSource in _controlSources)
                {
                    threads.Add(Task.Run(async () => await ProcessControlMessages(controlSource, dispatchQueue, t).ConfigureAwait(false)));
                }
                threads.Add(Task.Run(async () => await DispatchControlMessages(dispatchQueue, t).ConfigureAwait(false)));

                _activeProcess = Task.WhenAll(threads);
                await _activeProcess.ConfigureAwait(false);
            }
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        public void StopProcess()
        {
            //TODO: move to dispose and tear down system
            _ctSource.Cancel();
            _ctSource = new CancellationTokenSource();
            
        }

        private async Task ProcessControlMessages(IMessageSource<ControlMessage> controlSource, BlockingCollection<IMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    var message = controlSource.Take(t) ?? throw new Exception($"Received null from {controlSource.GetType()}.Take");

                    var results = await _deliverer.Deliver(message).ConfigureAwait(false);
                    foreach (var msg in results)
                    {
                        dispatchQueue.Add(msg);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //silence cancellation request exceptions
            }
            finally
            {
                dispatchQueue.CompleteAdding();
            }
        }

        private async Task DispatchControlMessages(BlockingCollection<IMessage> dispatchQueue, CancellationToken t)
        {
            try
            {
                foreach (var message in dispatchQueue.GetConsumingEnumerable(t))
                {
                    await _dispatcher.Dispatch(message, t).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
        }

    }
}
