using BlackSP.Infrastructure.Builders;
using BlackSP.Benchmarks.WordCount.Events;
using BlackSP.Benchmarks.WordCount.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.WordCount
{
    public static class Queries
    {
        public static void WordCount(IVertexGraphBuilder graphBuilder)
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
