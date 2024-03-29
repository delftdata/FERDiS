﻿using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Benchmarks.Graph.Models;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Operators;
using BlackSP.OperatorShells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Operators
{
    public class RankUpdateJoinOperator : IJoinOperator<AdjacencyEvent, PageEvent, PageUpdateEvent>
    {
        public TimeSpan WindowSize => Constants.RankUpdateWindowSize;

        public TimeSpan WindowSlideSize => Constants.RankUpdateWindowSlideSize;

        /// <summary>
        /// Representing a count as a double to not repeatedly cast to double during division in Map method
        /// </summary>
        private readonly double TotalPageCount;

        /// <summary>
        /// The probability that a PageRank surfer/walker traverses a link
        /// </summary>
        private readonly double DampeningFactor;

        /// <summary>
        /// The probability that a PageRank surfer/walker jumps to a random page
        /// </summary>
        private readonly double RandomJump;

        public RankUpdateJoinOperator()
        {
            string pageCountString = Environment.GetEnvironmentVariable("PR_PAGE_COUNT") ?? throw new ArgumentException("Missing environment variable PR_PAGE_COUNT");
            TotalPageCount = double.Parse(pageCountString);

            string dampeningString = Environment.GetEnvironmentVariable("PR_DAMPENING_FACTOR") ?? throw new ArgumentException("Missing environment variable PR_DAMPENING_FACTOR");
            DampeningFactor = double.Parse(dampeningString);

            if(DampeningFactor < 0 || DampeningFactor > 1)
            {
                throw new ArgumentOutOfRangeException($"Environment variable PR_DAMPENING_FACTOR out of range, must be [0, 1] but was {dampeningString}");
            }
            RandomJump = (1 - DampeningFactor) / TotalPageCount;
        }

        public bool Match(AdjacencyEvent testA, PageEvent testB) => testA.Adjacancy.PageId == testB.Page.PageId;
        
        public PageUpdateEvent Join(AdjacencyEvent matchA, PageEvent matchB)
        {
            var adjacency = matchA.Adjacancy;
            var page = matchB.Page;
            return new PageUpdateEvent
            {
                Key = 0,
                UpdatedPages = GetPages(adjacency, page).ToArray()
            };
        }

        private IEnumerable<Page> GetPages(Adjacency adjacency, Page page)
        {
            yield return new Page { PageId = page.PageId, Rank = RandomJump, Epoch = page.Epoch + 1 };

            if (adjacency.Neighbours == null)
            {
                yield break;
            }

            var neighboursRank = (DampeningFactor * page.Rank) / adjacency.Neighbours.Length;
            foreach (int neighbour in adjacency.Neighbours)
            {
                yield return new Page { PageId = neighbour, Rank = neighboursRank, Epoch = page.Epoch + 1 };
            }
        }

        
    }
}
