using BlackSP.Kernel.Operators;
using BlackSP.Sandbox.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Sandbox.Operators
{
    class SampleMapOperator : IMapOperator<SampleEvent, SampleEvent2>
    {
        public IEnumerable<SampleEvent2> Map(SampleEvent @event)
        {
            yield return new SampleEvent2(@event.Key, @event.EventTime, 1);
        }
    }

    
}
