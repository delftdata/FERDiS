using BlackSP.Kernel.Operators;
using BlackSP.Sandbox.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Sandbox.Operators
{
    class SampleFilterOperator : IFilterOperator<SampleEvent>
    {
        public SampleEvent Filter(SampleEvent @event)
        {
            return @event;
        }
    }

    
}
