using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class FilterOperator : BaseOperator
    {
        private readonly IFilterOperatorConfiguration _options;

        public FilterOperator(IFilterOperatorConfiguration options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            //TODO: test if returns empty enumerable when event gets filtered out
            var output = _options.Filter(@event);
            if(output != null) //ie. the @event did not get filtered out
            {
                yield return output;
            }
        }

    }
}
