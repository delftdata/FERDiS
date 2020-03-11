using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class MapOperator<TIn, TOut> : BaseOperator
        where TIn : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IMapOperatorConfiguration<TIn, TOut> _options;

        public MapOperator(IMapOperatorConfiguration<TIn, TOut> options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TIn ?? throw new ArgumentException($"Argument \"{nameof(@event)}\" was of type {@event.GetType()}, expected: {typeof(TIn)}");
            return _options.Map(typedEvent);
        }
    }
}
