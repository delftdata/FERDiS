using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class HopCountAggregateOperator : IAggregateOperator<HopEvent, HopEvent>
    {
        public TimeSpan WindowSize => Constants.HopCountWindowSize;
        public TimeSpan WindowSlideSize => Constants.HopCountWindowSlideSize;

        public IEnumerable<HopEvent> Aggregate(IEnumerable<HopEvent> window)
        {
            Console.WriteLine("WINDOW SIZE: " + window.Count());

            //shits highly inefficient, try paralel impl or other way to reduce complexity (currently is O(n^2)) real bad with 50.000 input

            var pairs = window.Select(e => (Event: e, Neighbours: window.Where(en => e.Neighbour.ToId == en.Neighbour.FromId && e.Neighbour.FromId != en.Neighbour.ToId)));
            foreach (var (Event, Neighbours) in pairs)
            {
                //join on matching from-to IDs to connect "hop-paths"
                //each match adds up the hopcounts per path segment
                foreach (var neighbour in Neighbours) 
                {
                    int hopCount = Event.Neighbour.Hops + neighbour.Neighbour.Hops;
                    int fromId = Event.Neighbour.FromId;
                    int toId = neighbour.Neighbour.ToId;
                    if (hopCount >= Constants.HopCountMax)
                    {
                        continue; //skip over anything outside the hop range or pointing to self
                    }
                    
                    yield return new HopEvent
                    {
                        Key = fromId.ToString(),
                        EventTime = Event.EventTime,
                        Neighbour = new Models.Neighbour { FromId = fromId, ToId = toId, Hops = hopCount }
                    };
                }

                //yield return Event;
            }
        }
    }
}
