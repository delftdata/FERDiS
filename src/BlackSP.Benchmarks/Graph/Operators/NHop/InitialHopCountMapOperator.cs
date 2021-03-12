using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Operators
{
    class InitialHopCountMapOperator : IMapOperator<AdjacencyEvent, HopEvent>
    {
        public IEnumerable<HopEvent> Map(AdjacencyEvent @event)
        {
            var adjacency = @event.Adjacancy;

            if(adjacency.Neighbours == null)
            {
                yield break;
            }

            foreach(var nb in adjacency.Neighbours)
            {
                var neighbour = new Neighbour { 
                    FromId = adjacency.PageId, 
                    ToId = nb, 
                    Hops = 1 
                };

                yield return new HopEvent
                {
                    Key = neighbour.FromId.ToString(),
                    EventTime = @event.EventTime,
                    Neighbour = neighbour
                };
            }
        }
    }
}
