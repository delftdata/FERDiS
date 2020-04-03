using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class AggregateOperator<TIn, TOut> : BaseWindowedOperator<TIn, TOut>
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        private readonly IAggregateOperatorConfiguration<TIn, TOut> _options;

        public AggregateOperator(IAggregateOperatorConfiguration<TIn, TOut> options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<TOut> ProcessClosedWindow(IEnumerable<TIn> closedWindow)
        {
            _ = closedWindow ?? throw new ArgumentNullException(nameof(closedWindow));
            return _options.Aggregate(closedWindow);
        }
    }
}
