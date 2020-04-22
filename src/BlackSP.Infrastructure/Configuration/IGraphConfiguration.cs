using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Configuration
{
    public interface IGraphConfiguration
    {
        void Configure(IOperatorGraphBuilder graph);
    }
}
