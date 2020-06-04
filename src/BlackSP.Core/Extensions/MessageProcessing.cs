using BlackSP.Kernel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Extensions
{
    public static class MessageProcessing
    {
        public static async Task ConnectAndStart(this IMessageReceiver receiver, IMessageDeliverer deliverer, CancellationToken t)
        {
            _ = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _ = deliverer ?? throw new ArgumentNullException(nameof(deliverer));

            var receivedMessages = receiver.GetReceivedMessageEnumerator(t);
            await deliverer.Deliver(receivedMessages, t).ConfigureAwait(false);
        }

        public static async Task ConnectAndStart(this IMessageDeliverer deliverer, IMessageDispatcher dispatcher, CancellationToken t)
        {
            _ = deliverer ?? throw new ArgumentNullException(nameof(deliverer));
            _ = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            var deliveredMessages = deliverer.GetDeliveryResultEnumerator(t);
            await dispatcher.Dispatch(deliveredMessages, t).ConfigureAwait(false);
        }
    }
}
