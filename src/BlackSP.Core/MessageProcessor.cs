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

        public MessageProcessor(
            IMessageReceiver messageReceiver,
            IMessageDeliverer messageDeliverer,
            IMessageDispatcher messageDispatcher)
        {
            _receiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
            _deliverer = messageDeliverer ?? throw new ArgumentNullException(nameof(messageDeliverer));
            _dispatcher = messageDispatcher ?? throw new ArgumentNullException(nameof(messageDispatcher));
        }

        public async Task StartSubsystems(CancellationToken t)
        {
            var exitedTask = await Task.WhenAny(
                    _receiver.ConnectAndStart(_deliverer, t), _deliverer.ConnectAndStart(_dispatcher, t)
                ).ConfigureAwait(false);

            await exitedTask.ConfigureAwait(false); 
        }


    }
}
