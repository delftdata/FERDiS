using BlackSP.Benchmarks.NEXMark;
using BlackSP.Benchmarks.NEXMark.Generator;
using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Models;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Required argument on position 0. Possible values: produce, blacksp");
                return;
            }
            switch (args[0])
            {
                case "produce": 
                    await ProduceNEXMarkAuctionData(); 
                    break;
                case "blacksp":
                    if(args.Length < 2)
                    {
                        throw new ArgumentException("2nd argument required for blacksp. Possible values: simulator, cra");
                    }
                    await RunBlackSP(args[1] == "simulator");
                    break;
                default:
                    break;
            }
        }

        static async Task ProduceNEXMarkAuctionData()
        {
            try
            {
                string brokerList = Environment.GetEnvironmentVariable("KAFKA_BROKERLIST");
                int genCalls = int.Parse(Environment.GetEnvironmentVariable("GENERATOR_CALLS"));
                string skipTopicList = Environment.GetEnvironmentVariable("GENERATOR_SKIP_TOPICS");
                await KafkaNEXMarkProducer.StartProductingAuctionData(genCalls, brokerList, skipTopicList);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            }
        }

        static async Task RunBlackSP(bool useSimulator)
        {
            var logTargets = (LogTargetFlags) int.Parse(Environment.GetEnvironmentVariable("LOG_TARGETS"));
            var logLevel = (LogEventLevel) int.Parse(Environment.GetEnvironmentVariable("LOG_LEVEL")); ;

            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Uncoordinated, true, 45))
                .ConfigureOperators(Queries.LocalItem)
                .Build();

            await app.RunAsync();
        }
    }
}
