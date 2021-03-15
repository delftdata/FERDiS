using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.OperatorShells.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class HopCountPartitionMapper : AutoCastingCycleOperatorBase<HopEvent>, IMapOperator<HopEvent, HopEvent>
    {

        private readonly ILogger _logger;

        [ApplicationState]
        private readonly IDictionary<int, IDictionary<int, int>> _neighbourDict;

        public HopCountPartitionMapper(ILogger logger)
        {
            _neighbourDict = new Dictionary<int, IDictionary<int, int>>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task Consume(HopEvent @event)
        {
            //update event arrived
            
            var neighbour = @event.Neighbour;
            if(Update(neighbour))
            {
                //does it matter that the update yielded a change?
            }
            return Task.CompletedTask;
        }

        public IEnumerable<HopEvent> Map(HopEvent @event)
        {
            var neighbour = @event.Neighbour;
            //new edge arrived.. forward it plus all known edges we can connect it to (where fromId == neighbour.toId)

            if(_neighbourDict.TryGetValue(neighbour.FromId, out var nbDict)) {
                if(nbDict.TryGetValue(neighbour.FromId, out var hopCount))
                {
                    if(hopCount < neighbour.Hops) //already seen edge
                    {
                        yield break; //without change
                    }
                    nbDict[neighbour.ToId] = neighbour.Hops; //with change
                }
            }
            @event.Key = null; //null key results in broadcast
            yield return @event;

            if (_neighbourDict.TryGetValue(neighbour.ToId, out var toNeighbours))
            {
                foreach (var pair in toNeighbours)
                {
                    var fromId = neighbour.ToId;
                    var toId = pair.Key;
                    var hops = pair.Value;
                    yield return new HopEvent
                    {
                        Key = null, //null key results in broadcast
                        EventTime = DateTime.Now,
                        Neighbour = new Neighbour { FromId = fromId, ToId = toId, Hops = hops }
                    };
                }
            }
        }

        private bool Update(Neighbour nb)
        {
            if (!_neighbourDict.ContainsKey(nb.FromId))
            {
                _neighbourDict.Add(nb.FromId, new Dictionary<int, int>());
            }

            var hopDict = _neighbourDict[nb.FromId];
            if (!hopDict.ContainsKey(nb.ToId))
            {
                hopDict[nb.ToId] = nb.Hops;
                return true;
            }
            else
            {
                int prevHopCount = hopDict[nb.ToId];
                int newHopCount = Math.Min(prevHopCount, nb.Hops);
                hopDict[nb.ToId] = newHopCount;

                return prevHopCount != newHopCount;
            }
        }
    }
}
