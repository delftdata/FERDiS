using BlackSP.Benchmarks.Events;
using BlackSP.Benchmarks.NEXMark;
using BlackSP.Benchmarks.NEXMark.Models;
using BlackSP.Benchmarks.Operators;
using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Models;
using Confluent.Kafka;
using Serilog.Events;
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
                await KafkaNEXMarkProducer.StartProductingAuctionData();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            }
        }

        static async Task RunBlackSP(bool useSimulator)
        {
            var logTargets = LogTargetFlags.Console | (useSimulator ? LogTargetFlags.File : LogTargetFlags.AzureBlob);
            var logLevel = LogEventLevel.Information;

            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Uncoordinated, true, 45))
                .ConfigureOperators((builder) => {
                    var source = builder.AddSource<BidSourceOperator, BidEvent>(3);
                    var filter = builder.AddFilter<Operators.Projection.BidFilterOperator, BidEvent>(3);
                    var sink = builder.AddSink<Operators.Projection.BidSinkOperator, BidEvent>(1);
                    source.Append(filter).AsPipeline();
                    filter.Append(sink);
                })
                .Build();

            await app.RunAsync();
        }

        
    }
}
