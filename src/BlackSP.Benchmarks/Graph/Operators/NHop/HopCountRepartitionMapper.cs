using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class HopCountRepartitionMapper : IMapOperator<HopEvent, HopEvent>
    {

        private readonly IVertexConfiguration _config;

        private readonly ILogger _logger;

        [ApplicationState]
        private readonly IDictionary<int, IDictionary<int, int>> _neighbourDict;


        public HopCountRepartitionMapper(IVertexConfiguration config, ILogger logger)
        {

            _neighbourDict = new Dictionary<int, IDictionary<int, int>>();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public IEnumerable<HopEvent> Map(HopEvent @event)
        {
            var nb = @event.Neighbour;
            bool partOfState = nb.FromId % _config.InstanceNames.Count() == _config.ShardId;
            //if (partOfState) //only one operator has edge as part of state 
            //{
                if(UpdateIfSmaller(nb.FromId, nb.ToId, nb.Hops))
                {
                    //there has been a change, output repartitioned to propagate update                    
                    @event.Key = nb.ToId;
                    yield return @event;
                }
                
            //}
            //regardless of being part of state.. find any edges that continue this edge's "path"..
            if(_neighbourDict.TryGetValue(nb.ToId, out var nbDict))
            {
                foreach(var entry in nbDict)
                {
                    int fromId = nb.FromId;
                    int toId = entry.Key;
                    int hops = entry.Value;

                    if(nb.Hops + hops <= Constants.HopCountMax) //only output within hop range
                    {
                        var nbout = new Neighbour { FromId = fromId, ToId = toId, Hops = nb.Hops + hops };
                        yield return new HopEvent { Key = toId, EventTime = @event.EventTime, Neighbour = nbout };
                    }
                }
                //_neighbourDict[nb.ToId] = new Dictionary<int, int>();
            }            
        }

        /// <summary>
        /// ToId as key and hopcount as value
        /// </summary>
        /// <param name="fromId"></param>
        /// <returns></returns>
        private IDictionary<int, int> GetNeighbours(int fromId)
        {
            if (!_neighbourDict.ContainsKey(fromId))
            {
                _neighbourDict.Add(fromId, new Dictionary<int, int>());
            }
            return _neighbourDict[fromId];
        }


        private bool UpdateIfSmaller(int fromId, int toId, int hopCount)
        {
            var neighbours = GetNeighbours(fromId);
            if (!neighbours.ContainsKey(toId))
            {
                neighbours[toId] = hopCount;
                return true;
            }
            int prevHopCount = neighbours[toId];
            int newHopCount = Math.Min(prevHopCount, hopCount);
            neighbours[toId] = newHopCount;
            return prevHopCount != newHopCount;
        }
        
    }
}
