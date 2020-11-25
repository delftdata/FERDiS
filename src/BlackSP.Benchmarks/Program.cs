using BlackSP.Benchmarks.NEXMark;
using BlackSP.Benchmarks.NEXMark.Models;
using Confluent.Kafka;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlackSP.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int generatorCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));
            string brokerList = Environment.GetEnvironmentVariable("KAFKA_BROKERLIST");
            
            var config = new ProducerConfig { BootstrapServers = brokerList, Partitioner = Partitioner.Consistent };
            using var producer = new ProducerBuilder<int, string>(config).Build();
            using var ctSource = new CancellationTokenSource();
            
            var startInfo = new ProcessStartInfo("java", $"-jar NEXMarkGenerator.jar -gen-calls {generatorCalls} -prettyprint false")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var generatorProcess = Process.Start(startInfo);
            var outPrinter = Task.Run(() => ParseXMLAndProduceToKafka(generatorProcess.StandardOutput, producer, ctSource.Token));
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

        static async Task ParseXMLAndProduceToKafka(StreamReader reader, IProducer<int, string> producer, CancellationToken token)
        {
            int generatorCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));

            //these numbers are approximations so the real count may end (somewhat) higher than the expected counts
            //this seems to be particularly true for people and auctions.
            double expectedPeopleCount = 50 + generatorCalls / 10;
            double expectedAuctionCount = 50 + generatorCalls;
            double expectedBidCount = generatorCalls * 10;

            int bidCount = 0;
            int peopleCount = 0;
            int auctionCount = 0;
            while(!reader.EndOfStream)
            {
                //XML reading begin
                var xmlHeader = reader.ReadLine();
                if(string.IsNullOrEmpty(xmlHeader))
                {
                    continue; //skip empty lines until we see an xml header
                }
                var xmlBody = reader.ReadLine();
                if(string.IsNullOrEmpty(xmlBody))
                {
                    throw new InvalidDataException("Empty xml body");
                }
                var xmlText = $"{xmlHeader}{xmlBody}";
                //XML reading end
                
                var xDoc = XDocument.Parse(xmlText);
                var parser = new XMLParser(xDoc);
                
                foreach(var person in parser.GetPeople())
                {
                    var message = new Message<int, string> { Key = person.Id, Value = JsonSerializer.Serialize(person) };
                    await producer.ProduceAsync(Person.KafkaTopicName, message, token);
                    peopleCount++;
                }

                foreach(var bid in parser.GetBids())
                {
                    var message = new Message<int, string> { Key = bid.AuctionId, Value = JsonSerializer.Serialize(bid) };
                    await producer.ProduceAsync(Bid.KafkaTopicName, message, token);
                    bidCount++;
                }

                foreach(var auction in parser.GetAuctions())
                {
                    var message = new Message<int, string> { Key = auction.Id, Value = JsonSerializer.Serialize(auction) };
                    await producer.ProduceAsync(Auction.KafkaTopicName, message, token);
                    auctionCount++;
                }

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
