using BlackSP.Infrastructure.Builders;
using BlackSP.StreamBench.Identity.Events;
using BlackSP.StreamBench.Identity.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.StreamBench
{
    partial class WorkloadConfiguration
    {

        internal static void ConfigureIdentity(IVertexGraphBuilder graph)
        {
            var source = graph.AddSource<IdentitySource, IdentityEvent>(1);
            var sink = graph.AddSink<IdentitySink, IdentityEvent>(1);

            source.Append(sink);
        }
    }
}
