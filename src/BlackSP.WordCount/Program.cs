﻿using BlackSP.Checkpointing;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using BlackSP.WordCount.Events;
using BlackSP.WordCount.Operators;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace BlackSP.WordCount
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var useSimulator = true;

            var logTargets = LogTargetFlags.Console;
            logTargets = logTargets | (useSimulator ? LogTargetFlags.File : LogTargetFlags.AzureBlob);

            var logLevel = LogEventLevel.Verbose;

            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(logTargets, logLevel))
                .ConfigureCheckpointing(new CheckpointConfiguration(CheckpointCoordinationMode.Coordinated, false))
                .ConfigureOperators(ConfigureOperatorGraph)
                .Build();

            await app.RunAsync();
        }

        static void ConfigureOperatorGraph(IOperatorVertexGraphBuilder graph)
        {
            var source = graph.AddSource<SentenceGeneratorSource, SentenceEvent>(1);
            var mapper = graph.AddMap<SentenceToWordMapper, SentenceEvent, WordEvent>(2);
            var reducer = graph.AddAggregate<WordCountAggregator, WordEvent, WordEvent>(2);
            var sink = graph.AddSink<WordCountLoggerSink, WordEvent>(1);

            source.Append(mapper);
            mapper.Append(reducer);
            reducer.Append(sink);
        }
    }
}