using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure
{
    public interface IVertexGraphBuilder
    {
        void ConfigureVertices(IOperatorVertexGraphBuilder graph);

        void ConfigureLogging();

        void ConfigureCheckpointing();
    }
}
