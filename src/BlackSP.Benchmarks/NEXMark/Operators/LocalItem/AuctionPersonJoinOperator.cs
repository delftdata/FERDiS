using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.LocalItem
{
    class AuctionPersonJoinOperator : IJoinOperator<AuctionEvent, PersonEvent, JoinEvent>
    {
        public TimeSpan WindowSize => TimeSpan.FromSeconds(10);
        public TimeSpan WindowSlideSize => TimeSpan.FromSeconds(5);

        public JoinEvent Join(AuctionEvent matchA, PersonEvent matchB)
        {
            return new JoinEvent
            {
                Key = string.Empty,
                EventTime = matchA.EventTime > matchB.EventTime ? matchA.EventTime : matchB.EventTime,
                EventA = matchA.Auction,
                EventB = matchB.Person
            };
        }

        public bool Match(AuctionEvent testA, PersonEvent testB)
        {
            return testA.Auction.PersonId == testB.Person.Id;
        }
    }
}
