using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph
{
    public class Constants
    {

        public static readonly TimeSpan RankUpdateWindowSize = TimeSpan.FromSeconds(6000);
        public static readonly TimeSpan RankUpdateWindowSlideSize = TimeSpan.FromSeconds(6000);

        public static readonly TimeSpan RankSumWindowSize = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan RankSumWindowSlideSize = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan HopCountWindowSize = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan HopCountWindowSlideSize = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// The N in N-hop neighbours
        /// </summary>
        public static readonly int HopCountMax = 3;

        /// <summary>
        /// Wether to show nhop result tuples in the logs
        /// </summary>
        public static readonly bool LogNhopOutput = false;

        /// <summary>
        /// The maximum amount of epochs any streaming pagerank element will make
        /// </summary>
        public static readonly int MaxEpochCount = 100;

        /// <summary>
        /// The amount of epochs to pass before sinking a new pagerank, useful for reducing the amount of sink output
        /// </summary>
        public static readonly int EpochSinkInterval = 5;

    }
}
