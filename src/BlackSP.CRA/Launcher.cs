using BlackSP.CRA.Configuration;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Utilities;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Configuration;
using CRA.ClientLibrary;
using CRA.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA
{
    
    
    public static class Launcher
    {
        /// <summary>
        /// Holds reference solely used during BlackSP startup
        /// </summary>
        private static IVertexGraphBuilder graphBuilder;

        /// <summary>
        /// Holds reference solely used during BlackSP startup
        /// </summary>
        private static IDataProvider userDataProvider;

        enum LaunchMode
        {
            ClusterSetup,
            LocalWorker
        }

        /// <summary>
        /// Primary entrypoint for BlackSP.CRA,
        /// </summary>
        /// <typeparam name="TConfiguration"></typeparam>
        /// <typeparam name="TDataProvider"></typeparam>
        /// <param name="args"></param>
        public static async Task LaunchWithAsync<TConfiguration, TDataProvider>(string[] args)
            where TConfiguration : IVertexGraphBuilder, new()
            where TDataProvider : IDataProvider, new()
        {
            EnforceEnvironmentVariables();
            userDataProvider = Activator.CreateInstance<TDataProvider>();//fix to azureprovider
            graphBuilder = Activator.CreateInstance<TConfiguration>();
            await LaunchAsync(args).ConfigureAwait(false);
        }

        private static async Task LaunchAsync(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Argument 0 missing: LaunchMode (0: Cluster mode, 1: Worker mode)");
                return;
            }

            LaunchMode launchMode = (LaunchMode)int.Parse(args[0]);
            switch (launchMode)
            {
                case LaunchMode.ClusterSetup:
                    await LaunchClusterSetupAsync();
                    break;
                case LaunchMode.LocalWorker:
                    LaunchLocalWorker(args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine("Invalid launch mode provided");
                    return;
            }
        }

        /// <summary>
        /// Launches cluster setup (depends on usergraph configuration being set)
        /// </summary>
        private static async Task LaunchClusterSetupAsync()
        {
            var craClientLibrary = new CRAClientLibrary(userDataProvider);
            var graphConfigurator = new CRAOperatorGraphBuilder(new KubernetesDeploymentUtility(), craClientLibrary);
            graphBuilder.ConfigureVertices(graphConfigurator); //pass configurator to user defined class
            await graphConfigurator.Build();
        }

        /// <summary>
        /// Launches a local worker with provided commandline arguments
        /// </summary>
        /// <param name="args"></param>
        private static void LaunchLocalWorker(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Worker mode has 2 required (r) and 1 optional (o) arguments: instanceName (r), portNumber (r), ipAddress (o)");
                return;
            }
            string instanceName = args[0];
            int portNum = int.Parse(args[1]);
            string ipAddress = args.Length == 3 ? args[2] : null;
            Worker.Launch(instanceName, portNum, userDataProvider, ipAddress);
        }

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
