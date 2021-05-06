using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.WordCount.Events;
using BlackSP.Benchmarks.WordCount.Operators;
using Confluent.Kafka;
using LoremNET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.WordCount.Generator
{
    public class LoremIpsumSentenceGenerator
    {

        public static void GenerateSentences()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent
            };

            using var producer = new ProducerBuilder<int, string>(config)
                //.SetValueSerializer(new ProtoBufAsyncValueSerializer<SentenceEvent>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Sentence produce error: {err}"))
                .Build();

            while(true)
            {
                //TODO: consider throttling to specified target throughput?

                var sentence = Lorem.Sentence(10, 20);
                //Console.WriteLine("GENERATED: " + sentence);
                var msg = new Message<int, string> { Key = sentence[0], Value = sentence, Timestamp = Timestamp.Default };

                producer.Produce("sentences", msg);
            }
            

        }

    }
}
