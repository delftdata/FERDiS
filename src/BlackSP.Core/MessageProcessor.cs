using BlackSP.Core.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IMessageReceiver _receiver;
        private readonly IMessageDeliverer _deliverer;
        private readonly IMessageDispatcher _dispatcher;
        private CancellationTokenSource _ctSource;

        public MessageProcessor(
            IMessageReceiver messageReceiver,
            IMessageDeliverer messageDeliverer,
            IMessageDispatcher messageDispatcher)
        {
            _receiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
            _deliverer = messageDeliverer ?? throw new ArgumentNullException(nameof(messageDeliverer));
            _dispatcher = messageDispatcher ?? throw new ArgumentNullException(nameof(messageDispatcher));
            _ctSource = new CancellationTokenSource();
        }

        public IEnumerable<Task> Start()
        {
            yield return _receiver.ConnectAndStart(_deliverer, _ctSource.Token)
                .ContinueWith(LogExceptionIfFaulted, TaskScheduler.Current);
            yield return _deliverer.ConnectAndStart(_dispatcher, _ctSource.Token)
                .ContinueWith(LogExceptionIfFaulted, TaskScheduler.Current);
        }

        public void Flush()
        {
            
            // _receiver - set flush mode (any input gets dropped)
            // _deliverer - nothing?
            // _generator - stop consuming? (low capacity queue will fill up and pause?)
            // _dispatcher - set flush mode (low capacity queue between partitioner and writer that can flush before adding)
        }

        public void Stop()
        {
            //TODO: move to dispose and tear down system
            _ctSource.Cancel();
            _ctSource = new CancellationTokenSource();
            
        }

        private static void LogExceptionIfFaulted(Task t)
        {
            if (t.IsFaulted)
            {
                Console.WriteLine(t.Exception);
            }
        }

    }
}
