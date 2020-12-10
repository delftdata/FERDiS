using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks
{
    public enum Benchmark
    {
        WordCount,
        Selection,
        LocalItem,
        HotItem,
        AverageSellingPriceBySeller
    }

    public static class BenchmarkExtensions
    {

        /// <summary>
        /// Retrieves the vertex graph builder configuration method for each Benchmark
        /// </summary>
        /// <param name="benchmark"></param>
        /// <returns></returns>
        public static Action<IVertexGraphBuilder> ConfigureGraph(this Benchmark benchmark)
        {
            switch(benchmark)
            {
                case Benchmark.WordCount:
                    return WordCount.Queries.WordCount;
                case Benchmark.Selection:
                    return NEXMark.Queries.Selection;
                case Benchmark.LocalItem:
                    return NEXMark.Queries.LocalItem;
                case Benchmark.HotItem:
                    return NEXMark.Queries.HotItem;
                case Benchmark.AverageSellingPriceBySeller:
                    return NEXMark.Queries.AverageSellingPriceBySeller;
                default:
                    throw new ArgumentException($"Unknown Benchmark number provided: {benchmark}");
            }
        }
    }
}
