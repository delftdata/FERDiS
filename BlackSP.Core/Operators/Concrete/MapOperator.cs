using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class MapOperator : BaseOperator
    {
        private readonly OnEvent<IEvent, IEnumerable<IEvent>> _userDelegate;

        public MapOperator(IMapOperatorConfiguration options) : base(options)
        {
            _userDelegate = options.Map;
        }
    }
}
