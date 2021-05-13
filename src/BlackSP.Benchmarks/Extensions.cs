using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks
{
    

    public static class JobExtensions
    {

        /// <summary>
        /// Retrieves the vertex graph builder configuration method for each Job
        /// </summary>
        /// <param name="benchmark"></param>
        /// <returns></returns>
        public static Action<IVertexGraphBuilder> ConfigureGraph(this Job benchmark, Size size)
        {
            switch(benchmark)
            {
                case Job.WordCount:
                    return WordCount.Queries.WordCount(size);
                case Job.Projection:
                    return WordCount.Queries.Projection(size);
                case Job.Selection:
                    return NEXMark.Queries.Selection(size);
                case Job.LocalItem:
                    return NEXMark.Queries.LocalItem(size);
                case Job.HotItem:
                    return NEXMark.Queries.HotItem(size);
                case Job.AverageSellingPriceBySeller:
                    return NEXMark.Queries.AverageSellingPriceBySeller(size);
                case Job.NHop:
                    return Graph.Queries.NHop(size);
                default:
                    throw new ArgumentException($"Unknown Benchmark number provided: {benchmark}");
            }
        }
    }
}
