using BlackSP.Infrastructure.Configuration;
using BlackSP.ThroughputExperiment.Events;
using CRA.DataProvider.Azure;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment
{
    public class Program
    {
        class ThroughputExperimentGraphConfiguration : IGraphConfiguration
        {
            public void Configure(IOperatorGraphBuilder graph)
            {
                var mapper = graph.AddMap<SampleMapOperatorConfiguration, SampleEvent, SampleEvent2>(2);
                var filter = graph.AddFilter<SampleFilterOperatorConfiguration, SampleEvent>(1);
                var filter2 = graph.AddFilter<SampleFilterOperatorConfiguration, SampleEvent>(2);
                filter.Append(filter2);
                filter2.Append(mapper);
            }
        }

        static async Task Main(string[] args)
        {
            //CRA runtime usage..
            //await BlackSP.CRA.Launcher.LaunchWithAsync<ThroughputExperimentGraphConfiguration, AzureDataProvider>(args);
            //In Memory runtime usage..
            await BlackSP.InMemory.Launcher.LaunchWithAsync< ThroughputExperimentGraphConfiguration>(args);
        }
    }
}
