using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.LocalItem
{
    class AuctionPersonJoinOperator : IJoinOperator<AuctionEvent, PersonEvent, AuctionPersonEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(30);
        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(5);

        public AuctionPersonEvent Join(AuctionEvent matchA, PersonEvent matchB)
        {
            return new AuctionPersonEvent
            {
                Key = matchA.Auction.Id,
                EventTime = matchA.EventTime > matchB.EventTime ? matchA.EventTime : matchB.EventTime,
                Auction = matchA.Auction,
                Person = matchB.Person
            };
        }

        public bool Match(AuctionEvent testA, PersonEvent testB)
        {
            return testA.Auction.PersonId == testB.Person.Id;
        }
    }
}
