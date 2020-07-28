using BlackSP.CRA.Configuration;
using BlackSP.CRA.Utilities;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Application;
using CRA.ClientLibrary;
using CRA.DataProvider.Azure;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA
{
    public static class Hosting
    {

        public static IApplicationBuilder CreateDefaultApplicationBuilder()
        {
            EnforceEnvironmentVariables();

            var azureProvider = new AzureDataProvider();
            var graphBuilder = new CRAOperatorGraphBuilder(new Kubernetes.KubernetesDeploymentUtility(), new CRAClientLibrary(azureProvider));
            return new ApplicationBuilder(graphBuilder);
        }

        //TODO: consider mapping CONN_STRING to CONNECTION_STRING just for CRA because its weird being named like this
        private static void EnforceEnvironmentVariables()
        {
            //Dirty hack because running with visual studio instrumentation
            //clears environment variables..
            if (Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING") == null)
            {
                Environment.SetEnvironmentVariable("AZURE_STORAGE_CONN_STRING", "DefaultEndpointsProtocol=https;AccountName=vertexstore;AccountKey=3BMGVlrXZq8+NE9caC47KDcpZ8X59vvxFw21NLNNLFhKGgmA8Iq+nr7naEd7YuGGz+M0Xm7dSUhgkUN5N9aMLw==;EndpointSuffix=core.windows.net");
            }
        }

    }
}
