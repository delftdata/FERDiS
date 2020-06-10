using BlackSP.Kernel;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Sources
{
    public class SourceOperatorDataSource : IMessageSource<DataMessage>
    {
        private readonly ISourceOperator<IEvent> _source;
        
        public SourceOperatorDataSource(ISourceOperator<IEvent> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Task Flush()
        {
            //There is nothing to flush
            return Task.CompletedTask;
        }

        public DataMessage Take(CancellationToken t)
        {
            IEvent next = _source.ProduceNext(t);
            return new DataMessage(next);
        }
    }
}
