﻿using BlackSP.Core.Extensions;
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
        private readonly IMessageSource<TMessage> _dataSource;
        private readonly IMessageDeliverer<TMessage> _deliverer;
        private readonly IDispatcher<TMessage> _dispatcher;

        private CancellationTokenSource _ctSource;
        private Task _activeProcess;

        public SingleSourceProcessController(
            IMessageSource<TMessage> dataSource,
            IMessageDeliverer<TMessage> dataDeliverer,
            IDispatcher<TMessage> dispatcher)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _deliverer = dataDeliverer ?? throw new ArgumentNullException(nameof(dataDeliverer));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            
            _ctSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Start core processes for data and control message processing
        /// </summary>
        public async Task StartProcess()
        {
            var t = _ctSource.Token;
            var dispatchQueue = new BlockingCollection<TMessage>(64);//TODO: determine proper capacity
            try
            {
                var deliveryThread = Task.Run(async () => await DeliverFromSource(dispatchQueue, t).ConfigureAwait(false));
                var dispatchThread = Task.Run(async () => await DispatchResults(dispatchQueue, t).ConfigureAwait(false));
                _activeProcess = Task.WhenAll(deliveryThread, dispatchThread);
                await _activeProcess.ConfigureAwait(false);
            } 
            finally
            {
                dispatchQueue.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Task that potentially throws is already awaited in StartProcess")]
        public async Task StopProcess()
        {
            if(_activeProcess == null) { throw new NotSupportedException("Cannot stop process that was not started"); }
            if(_ctSource.IsCancellationRequested) { throw new NotSupportedException("Cannot stop process that was already stopped"); }
            
            _ctSource.Cancel();
            try
            {
                //wait for active process to terminate
                await _activeProcess.ConfigureAwait(false); 
            } 
            catch { /*silence any exception*/ } 
            
            //now data source is no longer consumed it can be safely flushed
            await _dataSource.Flush().ConfigureAwait(false);

            _activeProcess = null;
            _ctSource = new CancellationTokenSource();
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
                _dispatcher.SetFlags(_dispatcher.GetFlags() | DispatchFlags.Data);
                foreach (var message in dispatchQueue.GetConsumingEnumerable(t))
                {
                    await _dispatcher.Dispatch(message, t).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { /*silence cancellation request exceptions*/ }
        }

    }

}