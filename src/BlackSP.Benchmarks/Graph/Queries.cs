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

        public static void NHop(IVertexGraphBuilder builder)
        {
            var source = builder.AddSource<RandomEdgeSourceOperator, HopEvent>(1);
            var partitionMapper = builder.AddMap<HopCountPartitionMapper, HopEvent, HopEvent>(3);
            var repartitionMapper = builder.AddMap<HopCountRepartitionMapper, HopEvent, HopEvent>(3);
            var sink = builder.AddSink<HopCountSinkOperator, HopEvent>(1);

            source.Append(partitionMapper);
            partitionMapper.Append(repartitionMapper);
            repartitionMapper.Append(partitionMapper).AsBackchannel(); 
            repartitionMapper.Append(sink);
        }
    }
}
