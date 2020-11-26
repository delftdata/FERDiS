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

namespace BlackSP.Benchmarks.NEXMark
{
    public class KafkaNEXMarkProducer
    {

        public static async Task StartProductingAuctionData()
        {
            int generatorCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));

            using var ctSource = new CancellationTokenSource();

            var startInfo = new ProcessStartInfo("java", $"-jar NEXMarkGenerator.jar -gen-calls {generatorCalls} -prettyprint false")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var generatorProcess = Process.Start(startInfo);
            var outPrinter = Task.Run(() => ParseXMLAndProduceToKafka(generatorProcess.StandardOutput, ctSource.Token));
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

        static async Task ParseXMLAndProduceToKafka(StreamReader reader, CancellationToken token)
        {
            int generatorCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));
            string brokerList = Environment.GetEnvironmentVariable("KAFKA_BROKERLIST");

            var config = new ProducerConfig { 
                BootstrapServers = brokerList, 
                Partitioner = Partitioner.Consistent
            };

            using var bidProducer = new ProducerBuilder<int, Bid>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Bid>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"KAFKA ERROR: {err}"))
                .Build();
            using var auctionProducer = new ProducerBuilder<int, Auction>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Auction>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"KAFKA ERROR: {err}"))
                .Build();
            using var peopleProducer = new ProducerBuilder<int, Person>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Person>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"KAFKA ERROR: {err}"))
                .Build();

            //these numbers are approximations so the real count may end (somewhat) higher than the expected counts
            //this seems to be particularly true for people and auctions.
            double expectedPeopleCount = 50 + generatorCalls / 10;
            double expectedAuctionCount = 50 + generatorCalls;
            double expectedBidCount = generatorCalls * 10;

            int bidCount = 0;
            int peopleCount = 0;
            int auctionCount = 0;
            while (!reader.EndOfStream)
            {
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

                var parser = new XMLParser($"{xmlHeader}{xmlBody}");
                var productTasks = new List<Task>();
                foreach (var person in parser.GetPeople())
                {
                    var message = new Message<int, Person> { Key = person.Id, Value = person };
                    //productTasks.Add(peopleProducer.ProduceAsync(Person.KafkaTopicName, message, token));
                    peopleCount++;
                }
                foreach (var bid in parser.GetBids())
                {
                    var message = new Message<int, Bid> { Key = bid.AuctionId, Value = bid };
                    productTasks.Add(bidProducer.ProduceAsync(Bid.KafkaTopicName, message, token));
                    bidCount++;
                }
                foreach (var auction in parser.GetAuctions())
                {
                    var message = new Message<int, Auction> { Key = auction.Id, Value = auction };
                    //productTasks.Add(auctionProducer.ProduceAsync(Auction.KafkaTopicName, message, token));
                    auctionCount++;
                }
                await Task.WhenAll(productTasks); //wait for all at once to allow higher throughput

                var auctionPercent = (int)Math.Round(auctionCount / expectedAuctionCount * 100);
                var peoplePercent = (int)Math.Round(peopleCount / expectedPeopleCount * 100);
                var bidPercent = (int)Math.Round(bidCount / expectedBidCount * 100);
                Console.WriteLine($"Auctions at ~{auctionPercent}%, People at ~{peoplePercent}%, Bids at ~{bidPercent}%");
            }

            Console.WriteLine($"-------------------------------------------------------------");
            Console.WriteLine($"Produced {peopleCount} People, {auctionCount} Auctions and {bidCount} Bids to Kafka");
        }

    }
}
