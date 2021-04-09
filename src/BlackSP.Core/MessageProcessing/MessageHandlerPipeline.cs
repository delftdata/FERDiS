using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageProcessing
{


    /// <summary>
    /// 
    /// </summary>
    public class MessageHandlerPipeline<T> : IPipeline<T> where T : IMessage
    {

        private readonly IEnumerable<IHandler<T>> _middlewares;
        private IVertexConfiguration _config;
        public MessageHandlerPipeline(IEnumerable<IHandler<T>> middlewares, IVertexConfiguration config)
        {
            _middlewares = middlewares ?? throw new ArgumentNullException(nameof(middlewares));
            if(!_middlewares.Any())
            {
                throw new ArgumentException($"{nameof(middlewares)} must have at least one element");
            }
            _config = config;
        }

        public Task<IEnumerable<T>> Process(T message)
        {          
            return ApplyMessageHandlers(message);           
        }

        private async Task<IEnumerable<T>> ApplyMessageHandlers(T message)
        {
            IEnumerable<T> results = await _middlewares.First().Handle(message).ConfigureAwait(false);
            foreach (var middleware in _middlewares.Skip(1))
            {
                var propagatedMessages = new List<T>();
                foreach(var msg in results)
                {
                    var nextMessages = await middleware.Handle(msg).ConfigureAwait(false) ?? throw new Exception($"Middleware of type {middleware.GetType()} returned null, expected IEnumerable");
                    propagatedMessages.AddRange(nextMessages);
                }
                results = propagatedMessages;

                if (!propagatedMessages.Any())
                {
                    break; // no need to continue iterating the handlers if current handler 'absorbed' the message
                }
            }
            return results;//.AsEnumerable();
        }
    }
}
