using BlackSP.CRA.Configuration;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Utilities;
using CRA.ClientLibrary;
using CRA.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.CRA
{
    public interface BlackSPGraphConfiguration
    {
        void Configure(IOperatorGraphConfigurator graph);
    }
    
    public static class BlackSPClient
    {
        /// <summary>
        /// Holds reference solely used during BlackSP startup
        /// </summary>
        private static BlackSPGraphConfiguration userGraphConfiguration;

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
        public static void LaunchWith<TConfiguration, TDataProvider>(string[] args)
            where TConfiguration : BlackSPGraphConfiguration, new()
            where TDataProvider : IDataProvider, new()
        {
            EnforceEnvironmentVariables();
            userDataProvider = Activator.CreateInstance<TDataProvider>();
            userGraphConfiguration = Activator.CreateInstance<TConfiguration>();
            Launch(args);
        }

        private static void Launch(string[] args)
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
                    LaunchClusterSetup();
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
        private static void LaunchClusterSetup()
        {
            var craClientLibrary = new CRAClientLibrary(userDataProvider);
            var graphConfigurator = new OperatorGraphConfigurator(new KubernetesDeploymentUtility(), craClientLibrary);
            userGraphConfiguration.Configure(graphConfigurator); //pass configurator to user defined class
            graphConfigurator.BuildGraph().Wait();
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
            string instanceName = args[1];
            int portNum = int.Parse(args[2]);
            string ipAddress = args.Length == 4 ? args[3] : null;
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
