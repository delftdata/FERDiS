﻿using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells
{
    public class SinkOperatorShell<TEvent> : OperatorShellBase 
        where TEvent : class, IEvent
    {
        private readonly ISinkOperator<TEvent> _pluggedInOperator;

        public SinkOperatorShell(ISinkOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));
        }

        public override async Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var tEvent = @event as TEvent ?? throw new ArgumentException($"Argument {nameof(@event)} was not of expected type {typeof(TEvent)}");
           await  _pluggedInOperator.Sink(tEvent).ConfigureAwait(false);
            return Enumerable.Empty<IEvent>();
        }

        

    }
}
