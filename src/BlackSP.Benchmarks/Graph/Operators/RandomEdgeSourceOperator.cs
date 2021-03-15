using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class RandomEdgeSourceOperator : ISourceOperator<HopEvent>
    {
        static int TotalVertexCount = 100000;

        public HopEvent ProduceNext(CancellationToken t)
        {
            //Task.Delay(1).Wait(); //TODO: remove nasty throttle


            Random r = new Random();
            int fromId = r.Next(1, TotalVertexCount);
            int toId = fromId;

            while(toId == fromId)
            {
                toId = r.Next(1, TotalVertexCount); //re-roll untill different
            }

            return new HopEvent
            {
                Key = fromId.ToString(),
                Neighbour = new Models.Neighbour
                {
                    FromId = fromId,
                    ToId = toId,
                    Hops = 1
                },
                EventTime = DateTime.Now
            };
        }
    }
}
