using BlackSP.Infrastructure.Builders;
using BlackSP.StreamBench.WordCount.Events;
using BlackSP.StreamBench.WordCount.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.StreamBench
{
    partial class WorkloadConfiguration
    {
        internal static void ConfigureWordCount(IVertexGraphBuilder graphBuilder)
        {
            var source = graphBuilder.AddSource<SentenceGeneratorSource, SentenceEvent>(1);
            var mapper = graphBuilder.AddMap<SentenceToWordMapper, SentenceEvent, WordEvent>(3);
            var reducer = graphBuilder.AddAggregate<WordCountAggregator, WordEvent, WordEvent>(2);
            var sink = graphBuilder.AddSink<WordCountLoggerSink, WordEvent>(2);

            source.Append(mapper);
            mapper.Append(reducer);
            reducer.Append(sink).AsPipeline();
        }


    }
}
