﻿using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.OperatorShells
{
    public class MapOperatorShell<TIn, TOut> : OperatorShellBase
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IMapOperator<TIn, TOut> _pluggedInOperator;

        public MapOperatorShell(IMapOperator<TIn, TOut> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TIn ?? throw new ArgumentException($"Argument \"{nameof(@event)}\" was of type {@event.GetType()}, expected: {typeof(TIn)}");
            return _pluggedInOperator.Map(typedEvent);
        }
    }
}