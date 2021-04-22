using BlackSP.Benchmarks.NEXMark;
using BlackSP.Benchmarks.NEXMark.Generator;
using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Configuration;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Required argument on position 0. Possible values: pagerank, nexmark, benchmark");
                    return;
                }
                switch (args[0])
                {
                    case "graph":
                        await ProduceGraphData();
                        break;
                    case "nexmark":
                        await ProduceNEXMarkAuctionData();
                        break;
                    case "benchmark":
                        var infrastructure = (Infrastructure)int.Parse(Environment.GetEnvironmentVariable("BENCHMARK_INFRA"));
                        var benchmark = (Job)int.Parse(Environment.GetEnvironmentVariable("BENCHMARK_JOB"));
                        var size = (Size)int.Parse(Environment.GetEnvironmentVariable("BENCHMARK_SIZE"));
                        await RunBenchmark(infrastructure, benchmark, size);
                        break;
                    default:
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
                string brokerList = Environment.GetEnvironmentVariable("KAFKA_BROKERLIST") ?? string.Empty;
                int genCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));
                string skipTopicList = Environment.GetEnvironmentVariable("GENERATOR_SKIP_TOPICS") ?? string.Empty;
                await KafkaNEXMarkProducer.StartProductingAuctionData(genCalls, brokerList, skipTopicList);
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
                string brokerList = Environment.GetEnvironmentVariable("KAFKA_BROKERLIST") ?? string.Empty;
                string edgesFileLoc = Environment.GetEnvironmentVariable("EDGES_FILE_LOCATION");
                await Graph.Producer.StartProductingGraphData(brokerList, edgesFileLoc);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            }
        }

        static async Task RunBenchmark(Infrastructure infrastructure, Job job, Size size)
        {
            LogTargetFlags logTargets = (LogTargetFlags) int.Parse(Environment.GetEnvironmentVariable("LOG_TARGET_FLAGS"));
            LogEventLevel logLevel = (LogEventLevel) int.Parse(Environment.GetEnvironmentVariable("LOG_EVENT_LEVEL"));

            CheckpointCoordinationMode checkpointCoordinationMode = (CheckpointCoordinationMode) int.Parse(Environment.GetEnvironmentVariable("CHECKPOINT_COORDINATION_MODE"));
            int checkpointIntervalSeconds = int.Parse(Environment.GetEnvironmentVariable("CHECKPOINT_INTERVAL_SECONDS"));
            bool allowStateReuse = checkpointCoordinationMode != CheckpointCoordinationMode.Coordinated;

            const string BOLD = "\x1B[1m";
            const string RESET = "\x1B[0m";

            Console.WriteLine($"Configuring BlackSP benchmark job {BOLD}{job}{RESET} ({BOLD}{size}{RESET}) on {BOLD}{infrastructure}{RESET} infrastructure");          
            Console.WriteLine($"Configuring checkpointing in {BOLD}{checkpointCoordinationMode}{RESET} mode on a {BOLD}{checkpointIntervalSeconds} seconds{RESET} interval");
            Console.WriteLine($"Configuring logging at level {BOLD}{logLevel}{RESET} to targets: {BOLD}{logTargets}{RESET}");

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
