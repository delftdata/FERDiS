using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.PageRank
{
    public static class Queries
    {

        public static void PageRank(IVertexGraphBuilder graphBuilder)
        {
            //source - string source (pid, n0, ..., nx)
            //map0 - mapper to adjacency (pid, [n0, ..., nx]) 

            //map1 - mapper to initial page (pid, rank)

            //map2 - mapper (stateful) to hold/overwrite page ranks

            //join - (map0, map2)
            //map3 - expand join results

            //aggregate - sum ranks from map3

            //sink - from map2, updates continuously, convergence can be checked by inspecting the output stream
        }
    }
}
