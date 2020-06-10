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

namespace BlackSP.Core
{    
    

    /// <summary>
    /// 
    /// </summary>
    public class MessageDeliverer : IMessageDeliverer
    {

        private readonly IEnumerable<IMiddleware> _middlewares;

        public MessageDeliverer(IEnumerable<IMiddleware> middlewares)
        {
            _middlewares = middlewares ?? throw new ArgumentNullException(nameof(middlewares));
            if(!_middlewares.Any())
            {
                throw new ArgumentException($"{nameof(middlewares)} must have at least one element");
            }
        }

        public async Task<IEnumerable<IMessage>> Deliver(IMessage message)
        {          
            return await ApplyDeliveryMiddlewares(message).ConfigureAwait(false);           
        }

        private async Task<IEnumerable<IMessage>> ApplyDeliveryMiddlewares(IMessage message)
        {
            IEnumerable<IMessage> results = await _middlewares.First().Handle(message).ConfigureAwait(false);
            foreach (var middleware in _middlewares.Skip(1))
            {
                var progatedMessages = new List<IMessage>();
                foreach(var msg in results)
                {
                    var nextMessages = await middleware.Handle(msg).ConfigureAwait(true) ?? throw new Exception($"Middleware of type {middleware.GetType()} returned null, expected IEnumerable");
                    progatedMessages.AddRange(nextMessages);
                }
                results = progatedMessages;

                if (!progatedMessages.Any())
                {
                    break; // no need to continue iterating the middlewares if the message sunk into a middleware
                }
            }
            //TODO: Layers
            //e.g. barrier blocking
            //e.g. cic clock updates & CP
            //e.g. uncoord. CP (count & invoke)
            //e.g. control message processing (do restore, should clear receiver and dispatcher)
            //e.g. operatorshell
            //e.g. heartbeat / metric collector
            //e.g. cic clock updates
            return results;//.AsEnumerable();
        }
    }
}
