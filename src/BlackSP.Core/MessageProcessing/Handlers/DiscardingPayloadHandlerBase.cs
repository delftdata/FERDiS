using BlackSP.Core.Extensions;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageProcessing.Handlers
{
    /// <summary>
    /// Base handler that attempts to extract the requested payload from a message and lets subclasses directly handle that payload.
    /// When the payload is not present on the message, the message will be discarded.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class DiscardingPayloadHandlerBase<TMessage, TPayload> : IHandler<TMessage>
        where TMessage : IMessage
        where TPayload : MessagePayloadBase
    {
        public async Task<IEnumerable<TMessage>> Handle(TMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if (!message.TryExtractPayload<TPayload>(out var payload))
            {
                return Enumerable.Empty<TMessage>();
            }

            //get results
            var results = await Handle(payload, t).ConfigureAwait(true);
            //only forward messages with payloads
            return results.Where(msg => msg.Payloads.Any());
        }

        /// <summary>
        /// Handle for processing requested payload from message, if any message without payloads is returned it will be discarded.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        protected abstract Task<IEnumerable<TMessage>> Handle(TPayload payload, CancellationToken t);
    }
}
