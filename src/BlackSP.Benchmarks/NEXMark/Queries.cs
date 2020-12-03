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
            var personFilter = builder.AddFilter<Operators.LocalItem.PersonLocationFilterOperator, PersonEvent>(1);
            personSource.Append(personFilter);

            var auctionSource = builder.AddSource<AuctionSourceOperator, AuctionEvent>(1);
            var auctionFilter = builder.AddFilter<Operators.LocalItem.AuctionCategoryFilterOperator, AuctionEvent>(1);
            auctionSource.Append(auctionFilter);

            var join = builder.AddJoin<Operators.LocalItem.AuctionPersonJoinOperator, AuctionEvent, PersonEvent, JoinEvent>(1);

            personFilter.Append(join);
            auctionFilter.Append(join);

            var sink = builder.AddSink<Operators.LocalItem.AuctionPersonSinkOperator, JoinEvent>(1);

            join.Append(sink);
        }

    }
}
