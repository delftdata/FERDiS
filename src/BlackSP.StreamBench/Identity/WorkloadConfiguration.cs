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
            //note current setup is sources and sinks in pipeline connection
            var source = graph.AddSource<IdentitySource, IdentityEvent>(2);
            var sink = graph.AddSink<IdentitySink, IdentityEvent>(2);

            source.Append(sink).AsPipeline();
        }
    }
}
