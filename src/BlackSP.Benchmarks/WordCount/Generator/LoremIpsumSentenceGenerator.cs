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

        public static async Task GenerateSentences()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent,
                //Debug = "msg"
            };

            using var producer = new ProducerBuilder<int, string>(config).SetStatisticsHandler((p, s) =>
            {
                Console.WriteLine(p);
                Console.WriteLine(s);
            })
                //.SetValueSerializer(new ProtoBufAsyncValueSerializer<SentenceEvent>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Sentence produce error: {err}"))
                .Build();

            int targetThroughput = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_TARGET_THROUGHPUT"));

            var windowAt = DateTime.UtcNow;
            var produceCounter = 0;
            while(true)
            {
                var nextWindow = windowAt.AddMilliseconds(1000);
                var now = DateTime.UtcNow;
                if (produceCounter > (targetThroughput) && nextWindow > now)
                {
                    Console.WriteLine($"produced {produceCounter} events, waiting for {(int)(nextWindow - now).TotalMilliseconds}ms (throttle)");
                    await Task.Delay(nextWindow - now);
                    producer.Flush();
                    continue;
                }

                if (nextWindow < now)
                {
                    Console.WriteLine($"resetting counter {produceCounter} to 0");
                    produceCounter = 0;
                    windowAt = nextWindow;
                }


                var sentence = Lorem.Sentence(1, 3);
                var msg = new Message<int, string> { Key = sentence[0], Value = sentence, Timestamp = Timestamp.Default };

                producer.ProduceAsync("sentences", msg);
                produceCounter++;
            }


        }

    }
}
