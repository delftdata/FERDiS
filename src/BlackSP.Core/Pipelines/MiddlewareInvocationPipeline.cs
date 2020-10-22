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

namespace BlackSP.Core.Pipelines
{    
    

    /// <summary>
    /// 
    /// </summary>
    public class MiddlewareInvocationPipeline<T> : IPipeline<T> where T : IMessage
    {

        private readonly IEnumerable<IHandler<T>> _middlewares;

        public MiddlewareInvocationPipeline(IEnumerable<IHandler<T>> middlewares)
        {
            _middlewares = middlewares ?? throw new ArgumentNullException(nameof(middlewares));
            if(!_middlewares.Any())
            {
                throw new ArgumentException($"{nameof(middlewares)} must have at least one element");
            }
        }

        public async Task<IEnumerable<T>> Process(T message)
        {          
            return await ApplyDeliveryMiddlewares(message).ConfigureAwait(false);           
        }

        private async Task<IEnumerable<T>> ApplyDeliveryMiddlewares(T message)
        {
            IEnumerable<T> results = await _middlewares.First().Handle(message).ConfigureAwait(false);
            foreach (var middleware in _middlewares.Skip(1))
            {
                var progatedMessages = new List<T>();
                foreach(var msg in results)
                {
                    var nextMessages = await middleware.Handle(msg).ConfigureAwait(false) ?? throw new Exception($"Middleware of type {middleware.GetType()} returned null, expected IEnumerable");
                    progatedMessages.AddRange(nextMessages);
                }
                results = progatedMessages;

                if (!progatedMessages.Any())
                {
                    break; // no need to continue iterating the middlewares if current middleware 'absorbed' the message
                }
            }
            return results;//.AsEnumerable();
        }
    }
}
