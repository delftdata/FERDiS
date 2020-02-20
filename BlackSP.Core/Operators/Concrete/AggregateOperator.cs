using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class AggregateOperator : BaseWindowedOperator
    {

        private readonly IAggregateOperatorConfiguration _options;

        public AggregateOperator(IAggregateOperatorConfiguration options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> ProcessClosedWindow(IEnumerable<IEvent> closedWindow)
        {
            return _options.Aggregate(closedWindow);
        }
    }
}
