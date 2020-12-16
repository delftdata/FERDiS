using BlackSP.Benchmarks.PageRank.Events;
using BlackSP.Benchmarks.PageRank.Operators;
using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.PageRank
{
    public static class Queries
    {

        public static void PageRank(IVertexGraphBuilder builder)
        {
            var source = builder.AddSource<AdjacencySourceOperator, AdjacencyEvent>(3);

            var initialRankMapper = builder.AddMap<InitialRankMapOperator, AdjacencyEvent, PageEvent>(3);
            source.Append(initialRankMapper);

            var collectionFilter = builder.AddFilter<RankCollectionFilterOperator, PageEvent>(2);
            initialRankMapper.Append(collectionFilter);

            var prJoin = builder.AddJoin<PageRankJoinOperator, AdjacencyEvent, PageEvent, PageUpdateEvent>(3);
            source.Append(prJoin);
            collectionFilter.Append(prJoin);

            var expansionMap = builder.AddMap<PageUpdateExpansionMapOperator, PageUpdateEvent, PageEvent>(3);
            prJoin.Append(expansionMap).AsPipeline(); //Note: is pipeline, join and expander need same shardcount

            var prSumAggregate = builder.AddAggregate<PageRankSumAggregateOperator, PageEvent, PageEvent>(2);
            expansionMap.Append(prSumAggregate);

            prSumAggregate.Append(collectionFilter); //this edge closes the cycle!

            var sink = builder.AddSink<PageRankSinkOperator, PageEvent>(1);

            collectionFilter.Append(sink);
        }
    }
}
