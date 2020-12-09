using BlackSP.Benchmarks.NEXMark;
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
                    throw new NotImplementedException();
                case Benchmark.Selection:
                    return Queries.Selection;
                case Benchmark.LocalItem:
                    return Queries.LocalItem;
                case Benchmark.HotItem:
                    return Queries.HotItem;
                case Benchmark.AverageSellingPriceBySeller:
                    return Queries.AverageSellingPriceBySeller;
                default:
                    throw new ArgumentException($"Unknown Benchmark number provided: {benchmark}");
            }
        }
    }
}
