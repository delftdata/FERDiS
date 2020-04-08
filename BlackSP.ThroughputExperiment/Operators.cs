using BlackSP.Core.Operators;
using BlackSP.ThroughputExperiment.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.ThroughputExperiment
{
    class SampleMapOperatorConfiguration : IMapOperatorConfiguration<SampleEvent, SampleEvent2>
    {
        public IEnumerable<SampleEvent2> Map(SampleEvent @event)
        {
            throw new System.NotImplementedException();
        }
    }

    class SampleFilterOperatorConfiguration : IFilterOperatorConfiguration<SampleEvent>
    {
        public SampleEvent Filter(SampleEvent @event)
        {
            throw new System.NotImplementedException();
        }
    }
}

