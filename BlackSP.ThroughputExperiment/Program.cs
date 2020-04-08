using BlackSP.Core.Operators;
using BlackSP.CRA;
using BlackSP.CRA.Configuration;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Utilities;
using BlackSP.ThroughputExperiment.Events;
using CRA.DataProvider;
using CRA.DataProvider.Azure;
using CRA.DataProvider.File;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment
{

    public class Program
    {

        class ThroughputExperimentGraphConfiguration : BlackSPConfiguration
        {
            public void ConfigureGraph(IOperatorGraphConfigurator graphConfigurator)
            {
                var mapper = graphConfigurator.AddMap<SampleMapOperatorConfiguration, SampleEvent, SampleEvent2>(2);
                var filter = graphConfigurator.AddFilter<SampleFilterOperatorConfiguration, SampleEvent>(1);
                filter.Append(mapper);
            }
        }

        static async Task Main(string[] args)
        {

            BlackSPClient.LaunchWith<ThroughputExperimentGraphConfiguration, AzureDataProvider>(args);            
        }
    }
}
