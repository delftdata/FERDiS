using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Operators
{
    class SamplerFilterOperator : IFilterOperator<HopEvent>
    {
        public HopEvent Filter(HopEvent @event)
        {
            var r = new Random();
            if(r.NextDouble() <= 0.2) //% that remains in the sample
            {
                return @event;
            }
            return null;
        }
    }
}
