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
        public static Action<IVertexGraphBuilder> WordCount(Size size)
        {
            int sourceShards = 1;
            int mapShards = 1;
            int reducerShards = 1;
            int sinkShards = 1;

            switch(size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 2;
                    mapShards = 3;
                    reducerShards = 3;
                    sinkShards = 3;
                    break;
                case Size.Large:
                    sourceShards = 4;
                    mapShards = 6;
                    reducerShards = 6;
                    sinkShards = 6;
                    break;
            }

            return (IVertexGraphBuilder graphBuilder) =>
            {
                var source = graphBuilder.AddSource<TestSentenceGeneratorSource, SentenceEvent>(sourceShards);
                //var source = graphBuilder.AddSource<KafkaSentenceSource, SentenceEvent>(sourceShards);
                
                var mapper = graphBuilder.AddMap<SentenceToWordMapper, SentenceEvent, WordEvent>(mapShards);
                var reducer = graphBuilder.AddAggregate<WordCountAggregator, WordEvent, WordEvent>(reducerShards);
                var sink = graphBuilder.AddSink<KafkaWordCountSink, WordEvent>(sinkShards);

                source.Append(mapper);
                mapper.Append(reducer);
                reducer.Append(sink).AsPipeline();
            };
            
        }

        public static Action<IVertexGraphBuilder> Projection(Size size)
        {
            int sourceShards = 1;
            int mapShards = 1;
            int sinkShards = 1;

            switch (size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 2;
                    mapShards = 3;
                    sinkShards = 3;
                    break;
                case Size.Large:
                    sourceShards = 4;
                    mapShards = 6;
                    sinkShards = 6;
                    break;
            }

            return (IVertexGraphBuilder graphBuilder) =>
            {
                //var source = graphBuilder.AddSource<TestSentenceGeneratorSource, SentenceEvent>(sourceShards);
                var source = graphBuilder.AddSource<KafkaSentenceSource, SentenceEvent>(sourceShards);

                var mapper = graphBuilder.AddMap<SentenceToWordMapper, SentenceEvent, WordEvent>(mapShards);
                var sink = graphBuilder.AddSink<KafkaWordCountSink, WordEvent>(sinkShards);

                source.Append(mapper);
                mapper.Append(sink);
            };

        }


    }
}
