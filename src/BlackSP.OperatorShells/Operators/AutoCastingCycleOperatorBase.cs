using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.Operators
{
    public abstract class AutoCastingCycleOperatorBase<TEvent> : ICycleOperator, ICycleOperator<TEvent>
        where TEvent : class, IEvent
    {
        public Task Consume(IEvent @event) 
            => Consume(@event as TEvent ?? throw new ArgumentException($"Unexpected type event of type: {@event.GetType()}, expected: {typeof(TEvent)}"));

        public abstract Task Consume(TEvent @event);
    }
}
