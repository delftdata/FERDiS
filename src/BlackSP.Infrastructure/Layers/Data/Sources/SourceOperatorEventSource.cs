using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using System;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.Endpoints;

namespace BlackSP.Infrastructure.Layers.Data.Sources
{
    public class SourceOperatorEventSource<TEvent> : ISource<DataMessage>
        where TEvent : class, IEvent
    {
        public (IEndpointConfiguration, int) MessageOrigin => (null, 0); //origin is local so no information to share


        private readonly ISourceOperator<TEvent> _source;
        
        public SourceOperatorEventSource(ISourceOperator<TEvent> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Task Flush()
        {
            //There is nothing to flush
            return Task.CompletedTask;
        }

        public Task<DataMessage> Take(CancellationToken t)
        {
            IEvent next = _source.ProduceNext(t);
            var payload = new EventPayload { Event = next };
            var res = new DataMessage(next.GetPartitionKey());
            res.AddPayload(payload);
            return Task.FromResult(res);
        }
    }
}
