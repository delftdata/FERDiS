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

        public static void Selection(IVertexGraphBuilder builder)
        {
            var source = builder.AddSource<BidSourceOperator, BidEvent>(3);
            var filter = builder.AddFilter<Operators.Projection.BidFilterOperator, BidEvent>(3);
            var sink = builder.AddSink<Operators.Projection.BidSinkOperator, BidEvent>(1);

            source.Append(filter).AsPipeline();
            filter.Append(sink);
        }

        public static void LocalItem(IVertexGraphBuilder builder)
        {
            var personSource = builder.AddSource<PersonSourceOperator, PersonEvent>(1);
            var auctionSource = builder.AddSource<AuctionSourceOperator, AuctionEvent>(1);

            var personFilter = builder.AddFilter<Operators.LocalItem.PersonLocationFilterOperator, PersonEvent>(1);
            personSource.Append(personFilter);

            var auctionFilter = builder.AddFilter<Operators.LocalItem.AuctionCategoryFilterOperator, AuctionEvent>(1);
            auctionSource.Append(auctionFilter);

            var join = builder.AddJoin<Operators.LocalItem.AuctionPersonJoinOperator, AuctionEvent, PersonEvent, AuctionPersonEvent>(1);
            personFilter.Append(join);
            auctionFilter.Append(join);

            var sink = builder.AddSink<Operators.LocalItem.AuctionPersonSinkOperator, AuctionPersonEvent>(1);
            join.Append(sink);
        }

        public static void HotItem(IVertexGraphBuilder builder)
        {
            var bidSource = builder.AddSource<BidSourceOperator, BidEvent>(3);

            var bidCounter = builder.AddAggregate<Operators.HotItem.BidCountAggregateOperator, BidEvent, BidCountEvent>(3);
            bidSource.Append(bidCounter);//.AsPipeline();

            var bidMaxCountFilter = builder.AddFilter<Operators.HotItem.MaxBidCountFilterOperator, BidCountEvent>(1);
            bidCounter.Append(bidMaxCountFilter);

            var bidSink = builder.AddSink<Operators.HotItem.BidCountSinkOperator, BidCountEvent>(1);
            bidMaxCountFilter.Append(bidSink);
        }

        public static void AverageSellingPriceBySeller(IVertexGraphBuilder builder)
        {
            var bidSource = builder.AddSource<BidSourceOperator, BidEvent>(3);
            var auctionSource = builder.AddSource<AuctionSourceOperator, AuctionEvent>(1);

            var bidAuctionJoin = builder.AddJoin<Operators.AverageSellingPriceBySeller.BidAuctionJoinOperator, BidEvent, AuctionEvent, BidAuctionEvent>(2);
            bidSource.Append(bidAuctionJoin);
            auctionSource.Append(bidAuctionJoin);

            var highestBidAggregate = builder.AddAggregate<Operators.AverageSellingPriceBySeller.HighestBidAggregateOperator, BidAuctionEvent, AuctionSellingPriceEvent>(2);
            bidAuctionJoin.Append(highestBidAggregate);

            var averageSellingPriceAggregate = builder.AddAggregate<Operators.AverageSellingPriceBySeller.AverageSellingPriceAggregateOperator, AuctionSellingPriceEvent, AveragePricePersonEvent>(2);
            highestBidAggregate.Append(averageSellingPriceAggregate);

            var sink = builder.AddSink<Operators.AverageSellingPriceBySeller.AveragePriceSinkOperator, AveragePricePersonEvent>(1);
            averageSellingPriceAggregate.Append(sink);
        }

    }
}
