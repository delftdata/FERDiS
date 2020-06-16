﻿using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageSources
{
    public class SourceOperatorDataSource<TEvent> : IMessageSource<DataMessage>
        where TEvent : class, IEvent
    {
        private readonly ISourceOperator<TEvent> _source;
        
        public SourceOperatorDataSource(ISourceOperator<TEvent> source)
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
