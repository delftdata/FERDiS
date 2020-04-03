using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class FilterOperator<TEvent> : BaseOperator 
        where TEvent : class, IEvent
    {
        private readonly IFilterOperatorConfiguration<TEvent> _options;

        public FilterOperator(IFilterOperatorConfiguration<TEvent> options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TEvent ?? throw new ArgumentException($"Argument \"{nameof(@event)}\" was of type {@event.GetType()}, expected: {typeof(TEvent)}");
            var output = _options.Filter(typedEvent);
            if(output != null) //ie. the @event did not get filtered out
            {
                yield return output;
            }
        }

    }
}
