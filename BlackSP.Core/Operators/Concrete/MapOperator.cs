using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class MapOperator : BaseOperator
    {
        private readonly IMapOperatorConfiguration _options;

        public MapOperator(IMapOperatorConfiguration options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            return _options.Map(@event);
        }
    }
}
