using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators.Concrete
{
    public class FilterOperator : BaseOperator
    {
        private readonly OnEvent<IEvent, IEvent> _userDelegate;

        public FilterOperator(IFilterOperatorConfiguration options) : base(options)
        {
            _userDelegate = options.Filter;
        }

    }
}
