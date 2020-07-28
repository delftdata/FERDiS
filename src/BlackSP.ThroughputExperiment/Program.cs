using BlackSP.CRA.Utilities;
using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using BlackSP.ThroughputExperiment.Events;
using CRA.DataProvider.Azure;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var useSimulator = false;

            //Worker.Launch("crainst01", 1500, new AzureDataProvider(), null);


            var appBuilder = useSimulator ? Simulator.Hosting.CreateDefaultApplicationBuilder() : CRA.Hosting.CreateDefaultApplicationBuilder();
            var app = await appBuilder
                .ConfigureLogging(new LogConfiguration(Kernel.Logging.LogTargetFlags.Console, Serilog.Events.LogEventLevel.Verbose))
                .ConfigureOperators(ConfigureOperatorGraph)
                .Build();

            await app.RunAsync();
        }

        static void ConfigureOperatorGraph(IOperatorVertexGraphBuilder graph)
        {
            var source = graph.AddSource<SampleSourceOperator, SampleEvent>(5);
            var filter = graph.AddFilter<SampleFilterOperator, SampleEvent>(1);
            var mapper = graph.AddMap<SampleMapOperator, SampleEvent, SampleEvent>(2);
            var aggregate = graph.AddAggregate<SampleAggregateOperator, SampleEvent, SampleEvent2>(1);
            var sink = graph.AddSink<SampleSinkOperator, SampleEvent>(2);

            if (true)
            {
                ///*
                source.Append(filter);
                filter.Append(mapper);
                //mapper.Append(aggregate);
                mapper.Append(sink);
                //*/
            }
            else
            {
                source.Append(sink);
            }
        }
    }
}
