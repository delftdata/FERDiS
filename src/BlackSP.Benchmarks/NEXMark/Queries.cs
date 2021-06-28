using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Benchmarks.NEXMark.Operators;
using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark
{
    public static class Queries
    {

        public static Action<IVertexGraphBuilder> Selection(Size size)
        {
            int sourceShards = 2;
            int filterShards = 2;
            int sinkShards = 2;
            switch(size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 8;
                    filterShards = 8;
                    sinkShards = 8;
                    break;
                case Size.Large:
                    sourceShards = 4;
                    filterShards = 8;
                    sinkShards = 4;
                    break;
            }


            return (IVertexGraphBuilder builder) =>
            {
                var source = builder.AddSource<BidSourceOperator, BidEvent>(sourceShards);
                var filter = builder.AddFilter<Operators.Projection.BidFilterOperator, BidEvent>(filterShards);
                var sink = builder.AddSink<Operators.Projection.BidSinkOperator, BidEvent>(sinkShards);

                source.Append(filter).AsPipeline();
                filter.Append(sink).AsPipeline();
            };
        }

        public static Action<IVertexGraphBuilder> LocalItem(Size size)
        {

            int pSourceShards = 2;
            int pFilterShards = 2;
            int aSourceShards = 2;
            int aFilterShards = 2;
            int joinShards = 2;
            int sinkShards = 2;
            switch (size)
            {
                case Size.Small: break;
                case Size.Medium:
                    pSourceShards = 4;
                    pFilterShards = 4;
                    aSourceShards = 4;
                    aFilterShards = 4;
                    joinShards = 4;
                    sinkShards = 4;
                    break;
                case Size.Large:
                    pSourceShards = 8;
                    pFilterShards = 8;
                    aSourceShards = 8;
                    aFilterShards = 8;
                    joinShards = 4;
                    sinkShards = 4;
                    break;
            }

            return (IVertexGraphBuilder builder) =>
            {
                var personSource = builder.AddSource<PersonSourceOperator, PersonEvent>(pSourceShards);
                var auctionSource = builder.AddSource<AuctionSourceOperator, AuctionEvent>(aSourceShards);

                var personFilter = builder.AddFilter<Operators.LocalItem.PersonLocationFilterOperator, PersonEvent>(pFilterShards);
                personSource.Append(personFilter).AsPipeline();

                var auctionFilter = builder.AddFilter<Operators.LocalItem.AuctionCategoryFilterOperator, AuctionEvent>(aFilterShards);
                auctionSource.Append(auctionFilter).AsPipeline();

                var join = builder.AddJoin<Operators.LocalItem.AuctionPersonJoinOperator, AuctionEvent, PersonEvent, AuctionPersonEvent>(joinShards);
                personFilter.Append(join);
                auctionFilter.Append(join);

                var sink = builder.AddSink<Operators.LocalItem.AuctionPersonSinkOperator, AuctionPersonEvent>(sinkShards);
                join.Append(sink).AsPipeline();
            };
            
        }

        public static Action<IVertexGraphBuilder> HotItem(Size size)
        {
            int sourceShards = 3;
            int aggregateShards = 3;
            int filterShards = 3;
            int sinkShards = 3;
            switch (size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 6;
                    aggregateShards = 6;
                    filterShards = 6;
                    sinkShards = 6;
                    break;
                case Size.Large:
                    sourceShards = 12;
                    aggregateShards = 12;
                    filterShards = 12;
                    sinkShards = 12;
                    break;
            }

            return (IVertexGraphBuilder builder) =>
            {
                var bidSource = builder.AddSource<BidSourceOperator, BidEvent>(sourceShards);

                var bidCounter = builder.AddAggregate<Operators.HotItem.BidCountAggregateOperator, BidEvent, BidCountEvent>(aggregateShards);
                bidSource.Append(bidCounter);//.AsPipeline();

                var bidMaxCountFilter = builder.AddFilter<Operators.HotItem.MaxBidCountFilterOperator, BidCountEvent>(filterShards);
                bidCounter.Append(bidMaxCountFilter);

                var bidSink = builder.AddSink<Operators.HotItem.BidCountSinkOperator, BidCountEvent>(sinkShards);
                bidMaxCountFilter.Append(bidSink);
            };

            
        }

        public static Action<IVertexGraphBuilder> AverageSellingPriceBySeller(Size size)
        {

            int bSourceShards = 2;
            int aSourceShards = 2;
            int joinShards = 2;
            int aggregate1Shards = 2;
            int aggregate2Shards = 2;
            int sinkShards = 2;
            switch (size)
            {
                case Size.Small: break;
                case Size.Medium:
                    bSourceShards = 3;
                    aSourceShards = 3;
                    joinShards = 3;
                    aggregate1Shards = 3;
                    aggregate2Shards = 3;
                    sinkShards = 3;
                    break;
                case Size.Large:
                    bSourceShards = 4;
                    aSourceShards = 4;
                    joinShards = 8;
                    aggregate1Shards = 4;
                    aggregate2Shards = 4;
                    sinkShards = 4;
                    break;
            }

            return (IVertexGraphBuilder builder) => {
                var bidSource = builder.AddSource<BidSourceOperator, BidEvent>(bSourceShards);
                var auctionSource = builder.AddSource<AuctionSourceOperator, AuctionEvent>(aSourceShards);

                var bidAuctionJoin = builder.AddJoin<Operators.AverageSellingPriceBySeller.BidAuctionJoinOperator, BidEvent, AuctionEvent, BidAuctionEvent>(joinShards);
                bidSource.Append(bidAuctionJoin);
                auctionSource.Append(bidAuctionJoin);

                var highestBidAggregate = builder.AddAggregate<Operators.AverageSellingPriceBySeller.HighestBidAggregateOperator, BidAuctionEvent, AuctionSellingPriceEvent>(aggregate1Shards);
                bidAuctionJoin.Append(highestBidAggregate);

                var averageSellingPriceAggregate = builder.AddAggregate<Operators.AverageSellingPriceBySeller.AverageSellingPriceAggregateOperator, AuctionSellingPriceEvent, AveragePricePersonEvent>(aggregate2Shards);
                highestBidAggregate.Append(averageSellingPriceAggregate);

                var sink = builder.AddSink<Operators.AverageSellingPriceBySeller.AveragePriceSinkOperator, AveragePricePersonEvent>(sinkShards);
                averageSellingPriceAggregate.Append(sink);
            };
            
        }

    }
}
