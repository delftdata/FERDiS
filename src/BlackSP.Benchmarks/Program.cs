﻿using BlackSP.Benchmarks.NEXMark;
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
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Required argument on position 0. Possible values: produce, blacksp");
                    return;
                }
                switch (args[0])
                {
                    case "pagerank":
                        await ProducePageRankAuctionData();
                        break;
                    case "nexmark":
                        await ProduceNEXMarkAuctionData();
                        break;
                    case "blacksp":
                        var infrastructure = (Infrastructure)int.Parse(Environment.GetEnvironmentVariable("BLACKSP_INFRASTRUCTURE"));
                        var benchmark = (Benchmark)int.Parse(Environment.GetEnvironmentVariable("BLACKSP_BENCHMARK"));
                        await RunBlackSP(infrastructure, benchmark);
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

        static async Task ProducePageRankAuctionData()
        {
            try
            {
                string brokerList = Environment.GetEnvironmentVariable("KAFKA_BROKERLIST") ?? string.Empty;
                string edgesFileLoc = Environment.GetEnvironmentVariable("EDGES_FILE_LOCATION");
                await PageRank.Producer.StartProductingGraphData(brokerList, edgesFileLoc);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Kafka auction data producer exited with exception");
                Console.WriteLine(e);
            }
        }

        static async Task RunBlackSP(Infrastructure infrastructure, Benchmark benchmark)
        {
            var logTargets = (LogTargetFlags) int.Parse(Environment.GetEnvironmentVariable("LOG_TARGETS"));
            var logLevel = (LogEventLevel) int.Parse(Environment.GetEnvironmentVariable("LOG_LEVEL")); ;

            var appBuilder = infrastructure == Infrastructure.Simulator 
                ? Simulator.Hosting.CreateDefaultApplicationBuilder() 
                : CRA.Hosting.CreateDefaultApplicationBuilder();

            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Uncoordinated, true, 450))
                .ConfigureOperators(benchmark.ConfigureGraph())
                .Build();

            await app.RunAsync();
        }
    }
}
