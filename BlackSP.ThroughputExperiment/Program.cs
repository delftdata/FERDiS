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

        class ThroughputExperimentGraphConfiguration : BlackSPGraphConfiguration
        {
            public void Configure(IOperatorGraphConfigurator graph)
            {
                var mapper = graph.AddMap<SampleMapOperatorConfiguration, SampleEvent, SampleEvent2>(2);
                var filter = graph.AddFilter<SampleFilterOperatorConfiguration, SampleEvent>(1);
                filter.Append(mapper);
            }
        }

        static void Main(string[] args)
        {
            BlackSPClient.LaunchWith<ThroughputExperimentGraphConfiguration, AzureDataProvider>(args);            
        }
    }
}
