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

        private readonly IEnumerable<IMessageMiddleware> _middlewares;
        private BlockingCollection<IMessage> _deliveryResultQueue;

        public MessageDeliverer(IEnumerable<IMessageMiddleware> middlewares)
        {
            _middlewares = middlewares ?? throw new ArgumentNullException(nameof(middlewares));
            if(!_middlewares.Any())
            {
                throw new ArgumentException($"{nameof(middlewares)} must have at least one element");
            }
            _deliveryResultQueue = new BlockingCollection<IMessage>();
        }

        public async Task Deliver(IEnumerable<IMessage> messages, CancellationToken t)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));

            var outputQueue = _deliveryResultQueue;
            foreach (var message in messages)
            {
                var middlewareResults = await ApplyDeliveryMiddlewares(message).ConfigureAwait(false);
                foreach(var msg in middlewareResults)
                {
                    outputQueue.Add(msg);
                }
            }
        }

        public IEnumerable<IMessage> GetDeliveryResultEnumerator(CancellationToken t)
        {
            return _deliveryResultQueue.GetConsumingEnumerable(t);
        }

        public void FlushDeliveryQueue()
        {
            _deliveryResultQueue.CompleteAdding();
        }

        public void ReinitializeDeliveryQueue()
        {
            _deliveryResultQueue = new BlockingCollection<IMessage>();
        }

        private async Task<IEnumerable<IMessage>> ApplyDeliveryMiddlewares(IMessage message)
        {
            IEnumerable<IMessage> results = _middlewares.First().Handle(message);
            foreach(var middleware in _middlewares.Skip(1))
            {
                results = results.SelectMany(msg => 
                    middleware.Handle(msg) ?? throw new Exception($"Middleware of type {middleware.GetType()} returned null, expected IEnumerable")
                );
                if(!results.Any())
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

        private void AddToResultQueue(IEnumerable<IMessage> messages)
        {
            foreach (var message in messages)
            {
                _deliveryResultQueue.Add(message);
            }
        }
    }
}
