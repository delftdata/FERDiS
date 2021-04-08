using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class LinearEdgeSourceOperator : ISourceOperator<HopEvent>
    {
        [ApplicationState]
        private int _lastVertexId = 0;

        public HopEvent ProduceNext(CancellationToken t)
        {
            Thread.Sleep(1000);
            Random r = new Random();
            int fromId = _lastVertexId;
            _lastVertexId++;
            int toId = _lastVertexId;

            return new HopEvent
            {
                Key = fromId,
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
