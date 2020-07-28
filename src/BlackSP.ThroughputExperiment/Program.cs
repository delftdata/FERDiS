using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Configuration;
using BlackSP.ThroughputExperiment.Events;
using CRA.DataProvider.Azure;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment
{
    public class Program
    {
        class ThroughputExperimentGraphConfiguration : IVertexGraphBuilder
        {
            public void ConfigureVertices(IOperatorVertexGraphBuilder graph)
            {
                var source = graph.AddSource<SampleSourceOperator, SampleEvent>(2);
                var filter = graph.AddFilter<SampleFilterOperator, SampleEvent>(1);
                var mapper = graph.AddMap<SampleMapOperator, SampleEvent, SampleEvent>(2);
                var aggregate = graph.AddAggregate<SampleAggregateOperator, SampleEvent, SampleEvent2>(1);
                var sink = graph.AddSink<SampleSinkOperator, SampleEvent>(2);

                if(true)
                {
                    ///*
                    source.Append(filter);
                    filter.Append(mapper);
                    //mapper.Append(aggregate);
                    mapper.Append(sink);
                    //*/
                } else
                {
                    source.Append(sink);
                }
            }

            public void ConfigureCheckpointing()
            {
                throw new System.NotImplementedException();
            }

            public void ConfigureLogging()
            {
                throw new System.NotImplementedException();
            }

        }

        static async Task Main(string[] args)
        {
            //CRA runtime usage..
            //await BlackSP.CRA.Launcher.LaunchWithAsync<ThroughputExperimentGraphConfiguration, AzureDataProvider>(args);
            
            //In Memory runtime usage..
            await BlackSP.Simulator.Launcher.LaunchWithAsync<ThroughputExperimentGraphConfiguration>(args);
        }
    }
}
