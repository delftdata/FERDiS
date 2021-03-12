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
    public class HopCountUpdateMapOperator : AutoCastingCycleOperatorBase<HopEvent>, IMapOperator<HopEvent, HopEvent>
    {

        private readonly ILogger _logger;

        [ApplicationState]
        private readonly IDictionary<int, IDictionary<int, int>> _neighbourDict;

        [ApplicationState]
        private List<HopEvent> _pendingEvents;

        public HopCountUpdateMapOperator(ILogger logger)
        {
            _neighbourDict = new Dictionary<int, IDictionary<int, int>>();
            _pendingEvents = new List<HopEvent>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task Consume(HopEvent @event)
        {
            var newResults = HandleEvent(@event).ToList();
            var count = newResults.Count();
            if (count > 0 && false)
            {
                _logger.Information($"Adding {count} new pending events");
                _pendingEvents.AddRange(newResults);
            }
        }

        public IEnumerable<HopEvent> Map(HopEvent @event)
        {
            var results = HandleEvent(@event);
            if(_pendingEvents.Count > 0 && false)
            {
                results = _pendingEvents.Concat(results);
                _logger.Information($"Flushing {_pendingEvents.Count} pending events");

                _pendingEvents = new List<HopEvent>();
            }
            return results;
        }

        private IEnumerable<HopEvent> HandleEvent(HopEvent @event)
        {
            var neighbour = @event.Neighbour;
            if (!_neighbourDict.ContainsKey(neighbour.FromId))
            {
                _neighbourDict.Add(neighbour.FromId, new Dictionary<int, int>());
            }
            bool hopCountChanged;
            var hopDict = _neighbourDict[neighbour.FromId];

            if (!hopDict.ContainsKey(neighbour.ToId))
            {
                hopDict[neighbour.ToId] = neighbour.Hops;
                hopCountChanged = true;
            }
            else
            {
                int prevHopCount = hopDict[neighbour.ToId];
                int newHopCount = Math.Min(prevHopCount, neighbour.Hops);
                hopDict[neighbour.ToId] = newHopCount;

                hopCountChanged = prevHopCount != newHopCount;
            }

            if (hopCountChanged)
            {
                yield return new HopEvent { Key = null, EventTime = @event.EventTime, Neighbour = neighbour };

                if(_neighbourDict.TryGetValue(neighbour.ToId, out var toNeighbours))
                {
                    foreach (var pair in toNeighbours)
                    {
                        var fromId = neighbour.ToId;
                        var toId = pair.Key;
                        var hops = pair.Value;
                        yield return new HopEvent
                        {
                            Key = null,
                            EventTime = DateTime.Now,
                            Neighbour = new Neighbour { FromId = fromId, ToId = toId, Hops = hops }
                        };
                    }
                }

                

            }
        }
    }
}
