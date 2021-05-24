using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.NEXMark.Models;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlackSP.Benchmarks.NEXMark.Generator
{
    public class KafkaNEXMarkProducer
    {

        public static async Task StartProductingAuctionData()
        {
            int generatorCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));
            int targetThroughput = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_TARGET_THROUGHPUT"));
            string skipTopicList = Environment.GetEnvironmentVariable("GENERATOR_SKIP_TOPICS") ?? string.Empty;


            using var ctSource = new CancellationTokenSource();
            Console.WriteLine($"Starting generator process \"java -jar NEXMarkGenerator.jar -gen-calls {generatorCalls}\"");
            var startInfo = new ProcessStartInfo("java", $"-jar NEXMarkGenerator.jar -gen-calls {generatorCalls} -prettyprint false")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var generatorProcess = Process.Start(startInfo);
            var outPrinter = Task.Run(() => ParseXMLAndProduceToKafka(generatorProcess.StandardOutput, generatorCalls, targetThroughput, skipTopicList, ctSource.Token));
            try
            {
                generatorProcess.WaitForExit();
            }
            catch
            {
                ctSource.Cancel();
            }
            finally
            {
                await outPrinter;
            }
        }

        static async Task ParseXMLAndProduceToKafka(StreamReader reader, int generatorCalls, int targetThroughput, string skipTopicList, CancellationToken token)
        {
            Console.WriteLine($"Instantiating kafka producers for topics {Bid.KafkaTopicName},{Auction.KafkaTopicName},{Person.KafkaTopicName}");
            if(!string.IsNullOrEmpty(skipTopicList))
            {
                Console.WriteLine($"Skipping kafka topics {skipTopicList}");
            }

            var config = new ProducerConfig { 
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(), 
                Partitioner = Partitioner.Consistent
            };

            using var bidProducer = new ProducerBuilder<int, Bid>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Bid>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Bid stream error: {err}"))
                .Build();
            using var auctionProducer = new ProducerBuilder<int, Auction>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Auction>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Auction stream error: {err}"))
                .Build();
            using var peopleProducer = new ProducerBuilder<int, Person>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Person>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Person stream error: {err}"))
                .Build();

            //these numbers are approximations so the real count may end (somewhat) higher than the expected counts
            //this seems to be particularly true for people and auctions.
            double expectedPeopleCount = 50 + generatorCalls / 10;
            double expectedAuctionCount = 50 + generatorCalls;
            double expectedBidCount = generatorCalls * 10;

            int bidCount = 0;
            int peopleCount = 0;
            int auctionCount = 0;

            Console.WriteLine("Beginning to parse generator data and produce it to kafka topics");
            var windowAt = DateTime.UtcNow;
            var produceCounter = 0;
            int i = 0;
            while (!reader.EndOfStream)
            {
                //throttling begin
                var nextWindow = windowAt.AddMilliseconds(100);
                var now = DateTime.UtcNow;
                if (produceCounter > (targetThroughput / 10) && nextWindow > now)
                {
                    Console.WriteLine($"produced {produceCounter} events, waiting for {(int)(nextWindow - now).TotalMilliseconds}ms (throttle)");
                    await Task.Delay(nextWindow - now);
                    continue;
                }

                if (nextWindow < now)
                {
                    Console.WriteLine($"resetting counter {produceCounter} to 0");
                    produceCounter = 0;
                    windowAt = nextWindow;
                }
                //throttling end


                //XML reading begin
                var xmlHeader = reader.ReadLine();
                if (string.IsNullOrEmpty(xmlHeader))
                {
                    continue; //skip empty lines until we see an xml header
                }
                var xmlBody = reader.ReadLine();
                if (string.IsNullOrEmpty(xmlBody))
                {
                    throw new InvalidDataException("Empty xml body");
                }
                //XML reading end


                //data producing begin
                var parser = new NEXMarkXMLParser($"{xmlHeader}{xmlBody}");
                var produceTasks = new List<Task>();
                if(!skipTopicList.Contains(Person.KafkaTopicName))
                {
                    foreach (var person in parser.GetPeople())
                    {
                        var message = new Message<int, Person> { Key = person.Id, Value = person };
                        produceTasks.Add(peopleProducer.ProduceAsync(Person.KafkaTopicName, message, token));
                        peopleCount++;
                    }
                }

                if (!skipTopicList.Contains(Bid.KafkaTopicName))
                {
                    foreach (var bid in parser.GetBids())
                    {
                        var message = new Message<int, Bid> { Key = bid.AuctionId, Value = bid };
                        produceTasks.Add(bidProducer.ProduceAsync(Bid.KafkaTopicName, message, token));
                        bidCount++;
                    }
                }
                if (!skipTopicList.Contains(Auction.KafkaTopicName))
                {
                    foreach (var auction in parser.GetAuctions())
                    {
                        var message = new Message<int, Auction> { Key = auction.Id, Value = auction };
                        produceTasks.Add(auctionProducer.ProduceAsync(Auction.KafkaTopicName, message, token));
                        auctionCount++;
                    }
                }

                produceCounter += produceTasks.Count;
                //dont actually await producetasks
                //data producing end



                //some progress tracking
                var auctionPercent = (int)Math.Round(auctionCount / expectedAuctionCount * 100);
                var peoplePercent = (int)Math.Round(peopleCount / expectedPeopleCount * 100);
                var bidPercent = (int)Math.Round(bidCount / expectedBidCount * 100);
                if(i % (generatorCalls/100) == 0)
                {
                    Console.WriteLine($"Auctions at ~{auctionPercent}%, People at ~{peoplePercent}%, Bids at ~{bidPercent}%");
                }
                i++;
            }
            Console.WriteLine($"-------------------------------------------------------------");
            Console.WriteLine($"Produced {peopleCount} People, {auctionCount} Auctions and {bidCount} Bids to Kafka");
        }

    }
}
