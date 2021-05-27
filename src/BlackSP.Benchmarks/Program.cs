using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.MetricCollection;
using BlackSP.Benchmarks.NEXMark;
using BlackSP.Benchmarks.NEXMark.Generator;
using BlackSP.Benchmarks.WordCount.Generator;
using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Configuration;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("nl-NL");
            CultureInfo.CurrentUICulture = CultureInfo.CreateSpecificCulture("nl-NL");

#if TRACE && false
            Environment.SetEnvironmentVariable("AZURE_STORAGE_CONN_STRING", "DefaultEndpointsProtocol=https;AccountName=vertexstore;AccountKey=3BMGVlrXZq8+NE9caC47KDcpZ8X59vvxFw21NLNNLFhKGgmA8Iq+nr7naEd7YuGGz+M0Xm7dSUhgkUN5N9aMLw==;EndpointSuffix=core.windows.net");
            Environment.SetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;");
            Environment.SetEnvironmentVariable("KAFKA_BROKER_DNS_TEMPLATE", "localhost:3240{0}");
            Environment.SetEnvironmentVariable("KAFKA_BROKER_COUNT", "4");
            Environment.SetEnvironmentVariable("LOG_TARGET_FLAGS", "1");
            Environment.SetEnvironmentVariable("LOG_EVENT_LEVEL", "2");
            Environment.SetEnvironmentVariable("CHECKPOINT_COORDINATION_MODE", "0");
            Environment.SetEnvironmentVariable("CHECKPOINT_INTERVAL_SECONDS", "30");
            Environment.SetEnvironmentVariable("BENCHMARK_INFRA", "0");
            Environment.SetEnvironmentVariable("BENCHMARK_JOB", "1");
            Environment.SetEnvironmentVariable("BENCHMARK_SIZE", "0");
#endif

            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Required argument on position 0. Possible values: delete-topic, graph, text, nexmark, benchmark");
                    return;
                }
                switch (args[0])
                {                        
                    //TODO: add option to clear blob-storage (logs) 

                    case "delete-topic":
                        await DeleteKafkaTopics();
                        break;
                    case "text":
                        await ProduceTextData();
                        break;
                    case "graph":
                        await ProduceGraphData();
                        break;
                    case "nexmark":
                        await ProduceNEXMarkAuctionData();
                        break;
                    case "throughput":
                        CollectThroughputMetrics();
                        break;
                    case "latency":
                        CollectLatencyMetrics();
                        break;
                    case "benchmark":
                        var infrastructure = (Infrastructure)int.Parse(Environment.GetEnvironmentVariable("BENCHMARK_INFRA"));
                        var benchmark = (Job)int.Parse(Environment.GetEnvironmentVariable("BENCHMARK_JOB"));
                        var size = (Size)int.Parse(Environment.GetEnvironmentVariable("BENCHMARK_SIZE"));
                        await RunBenchmark(infrastructure, benchmark, size);
                        break;
                    case "consume":
                        new TestConsumer().Consume();
                        break;
                    default:
                        Console.WriteLine($"Unknown argument on position 0: {args[0]}. Possible values: delete-topic, graph, text, nexmark, benchmark");
                        break;
                }
            } 
            catch(Exception e)
            {
                Console.WriteLine($"Benchmarks exited with an exception\n{e}");
                throw;
            }
        }

        static async Task ProduceNEXMarkAuctionData()
        {
            try
            {
                await KafkaNEXMarkProducer.StartProductingAuctionData();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            }
        }

        static async Task ProduceGraphData()
        {
            try
            {
                string edgesFileLoc = Environment.GetEnvironmentVariable("EDGES_FILE_LOCATION");
                await Graph.Producer.StartProductingGraphData(edgesFileLoc);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            }
        }

        static async Task ProduceTextData()
        {
            try
            {
                Console.WriteLine("Starting text data generator");
                await LoremIpsumSentenceGenerator.GenerateSentences();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            } 
            finally
            {
                Console.WriteLine("Exiting text data generator");
            }
        }

        static void CollectThroughputMetrics()
        {
            Console.WriteLine("Starting throughput metric collection");
            var collector = new ThroughputCalculatingConsumer();
            collector.Start();
        }

        static void CollectLatencyMetrics()
        {
            Console.WriteLine("Starting latency metric collection");
            var collector = new E2ELatencyCalculatingConsumer();
            collector.Start();
        }

        static async Task DeleteKafkaTopics()
        {
            Console.WriteLine($"Deleting all Kafka topics");
            await KafkaUtils.DeleteAllKafkaTopics();
            Console.WriteLine($"Deleted all Kafka topics");
        }



        static async Task RunBenchmark(Infrastructure infrastructure, Job job, Size size)
        {
            LogTargetFlags logTargets = (LogTargetFlags) int.Parse(Environment.GetEnvironmentVariable("LOG_TARGET_FLAGS"));
            LogEventLevel logLevel = (LogEventLevel) int.Parse(Environment.GetEnvironmentVariable("LOG_EVENT_LEVEL"));

            CheckpointCoordinationMode checkpointCoordinationMode = (CheckpointCoordinationMode) int.Parse(Environment.GetEnvironmentVariable("CHECKPOINT_COORDINATION_MODE"));
            int checkpointIntervalSeconds = int.Parse(Environment.GetEnvironmentVariable("CHECKPOINT_INTERVAL_SECONDS"));
            bool allowStateReuse = checkpointCoordinationMode != CheckpointCoordinationMode.Coordinated;


            Console.WriteLine($"Configuring BlackSP benchmark job {job} ({size}) on {infrastructure} infrastructure");          
            Console.WriteLine($"Configuring checkpointing in {checkpointCoordinationMode} mode on a {checkpointIntervalSeconds} seconds interval");
            Console.WriteLine($"Configuring logging at level {logLevel} to targets: {logTargets}");
            Console.WriteLine("\n");
            var appBuilder = infrastructure == Infrastructure.Simulator 
                ? Simulator.Hosting.CreateDefaultApplicationBuilder() 
                : CRA.Hosting.CreateDefaultApplicationBuilder();

            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(checkpointCoordinationMode, allowStateReuse, checkpointIntervalSeconds))
                .ConfigureOperators(job.ConfigureGraph(size))
                .Build();

            await app.RunAsync();
        }
    }
}
