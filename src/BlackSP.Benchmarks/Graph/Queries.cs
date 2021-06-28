using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Operators;
using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph
{
    public static class Queries
    {

        [Obsolete("Pagerank was deprecated due to not being true cylic streaming")]
        public static void PageRank(IVertexGraphBuilder builder)
        {
            //provides stream of vertice-neighbours pairs
            var source = builder.AddSource<AdjacencySourceOperator, AdjacencyEvent>(1);

            //applies initial ranks to vertices
            var initialRanker = builder.AddMap<InitialRankMapOperator, AdjacencyEvent, PageEvent>(1);
            source.Append(initialRanker);

            //collects/forwards ranks and forms the entry/exit point of the pagerank update loop
            var rankCollecter = builder.AddFilter<RankCollectFilterOperator, PageEvent>(1);
            initialRanker.Append(rankCollecter);

            //updates page ranks
            var rankUpdater = builder.AddJoin<RankUpdateJoinOperator, AdjacencyEvent, PageEvent, PageUpdateEvent>(1);
            source.Append(rankUpdater);
            rankCollecter.Append(rankUpdater);

            //expands page rank update events into sets of new pageranks
            var updateExpander = builder.AddMap<PageUpdateExpansionMapOperator, PageUpdateEvent, PageEvent>(1);
            rankUpdater.Append(updateExpander).AsPipeline(); //Note: is pipeline, join and expander need same shardcount

            //sums pageranks
            var rankSummer = builder.AddAggregate<RankSumAggregateOperator, PageEvent, PageEvent>(1);
            updateExpander.Append(rankSummer);

            //end of pagerank update, cycle back to collecter (this edge closes the cycle)
            rankSummer.Append(rankCollecter);

            //receives updates from the collecter and sinks the results to the log
            var sink = builder.AddSink<PageRankSinkOperator, PageEvent>(1);
            rankCollecter.Append(sink);
        }


        public static Action<IVertexGraphBuilder> NHop(Size size)
        {
            int sourceShards = 3;
            int partitionMapShards = 2;
            int repartitionMapShards = 2;
            int sinkShards = 3;
            switch (size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 6;
                    partitionMapShards = 6;
                    repartitionMapShards = 6;
                    sinkShards = 6;
                    break;
                case Size.Large:
                    sourceShards = 4;
                    partitionMapShards = 4;
                    repartitionMapShards = 4;
                    sinkShards = 4;
                    break;
            }

            return (IVertexGraphBuilder builder) =>
            {
                var source = builder.AddSource<NeighbourSourceOperator, HopEvent>(sourceShards);
                var partitionMapper = builder.AddMap<HopCountPartitionMapper, HopEvent, HopEvent>(partitionMapShards);
                var repartitionMapper = builder.AddMap<HopCountRepartitionMapper, HopEvent, HopEvent>(repartitionMapShards);
                var sink = builder.AddSink<HopCountSinkOperator, HopEvent>(sinkShards);

                source.Append(partitionMapper);
                partitionMapper.Append(repartitionMapper).AsPipeline();
                repartitionMapper.Append(partitionMapper).AsBackchannel();
                repartitionMapper.Append(sink).AsPipeline();
            };
            
        }
    }
}
