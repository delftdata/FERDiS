using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Configuration
{
    public interface IGraphConfigurator
    {
        void Configure(IOperatorGraphBuilder graph);
    }
}
