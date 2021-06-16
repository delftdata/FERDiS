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
            int sourceShards = 2;
            int mapShards = 2;
            int reducerShards = 2;
            int sinkShards = 2;

            switch(size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 12;
                    mapShards = 12;
                    reducerShards = 4;
                    sinkShards = 4;
                    break;
                case Size.Large:
                    sourceShards = 4;
                    mapShards = 4;
                    reducerShards = 4;
                    sinkShards = 4;
                    break;
            }

            return (IVertexGraphBuilder graphBuilder) =>
            {
                //var source = graphBuilder.AddSource<TestSentenceGeneratorSource, SentenceEvent>(sourceShards);
                var source = graphBuilder.AddSource<KafkaSentenceSource, SentenceEvent>(sourceShards);
                
                var mapper = graphBuilder.AddMap<SentenceToWordMapper, SentenceEvent, WordEvent>(mapShards);
                var reducer = graphBuilder.AddAggregate<WordCountAggregator, WordEvent, WordEvent>(reducerShards);
                var sink = graphBuilder.AddSink<KafkaWordCountSink, WordEvent>(sinkShards);

                source.Append(mapper).AsPipeline();
                mapper.Append(reducer);
                reducer.Append(sink).AsPipeline();
            };
            
        }

        public static Action<IVertexGraphBuilder> Projection(Size size)
        {
            int sourceShards = 4;
            int mapShards = 4;
            int sinkShards = 2;

            switch (size)
            {
                case Size.Small: break;
                case Size.Medium:
                    sourceShards = 12;
                    mapShards = 12;
                    sinkShards = 6;
                    break;
                case Size.Large:
                    sourceShards = 24;
                    mapShards = 24;
                    sinkShards = 24;
                    break;
            }

            return (IVertexGraphBuilder graphBuilder) =>
            {
                //var source = graphBuilder.AddSource<TestSentenceGeneratorSource, SentenceEvent>(sourceShards);
                var source = graphBuilder.AddSource<KafkaSentenceSource, SentenceEvent>(sourceShards);

                var mapper = graphBuilder.AddMap<SentenceToWordMapper, SentenceEvent, WordEvent>(mapShards);
                var sink = graphBuilder.AddSink<KafkaWordCountSink, WordEvent>(sinkShards);

                source.Append(mapper).AsPipeline();
                mapper.Append(sink);
            };

        }


    }
}
