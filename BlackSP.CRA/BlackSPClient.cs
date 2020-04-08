using BlackSP.CRA.Configuration;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.Utilities;
using CRA.ClientLibrary;
using CRA.DataProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA
{
    public static class BlackSPClient
    {
        public static void LaunchWith<TConfiguration, TDataProvider>(string[] args)
            where TConfiguration : BlackSPConfiguration, new()
            where TDataProvider : IDataProvider, new()
        {
            //Dirty hack because running with visual studio instrumentation
            //clears environment variables..
            if (Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING") == null)
            {
                Environment.SetEnvironmentVariable("AZURE_STORAGE_CONN_STRING", "DefaultEndpointsProtocol=https;AccountName=vertexstore;AccountKey=3BMGVlrXZq8+NE9caC47KDcpZ8X59vvxFw21NLNNLFhKGgmA8Iq+nr7naEd7YuGGz+M0Xm7dSUhgkUN5N9aMLw==;EndpointSuffix=core.windows.net");
            }

            var craDataProvider = Activator.CreateInstance<TDataProvider>();
            var userGraphConfiguration = Activator.CreateInstance<TConfiguration>();

            StartApplication(args, userGraphConfiguration, craDataProvider);
        }

        private static void StartApplication(string[] args, BlackSPConfiguration userGraphConfiguration, IDataProvider craDataProvider)
        {
            


            if (args.Length < 1)
            {
                Console.WriteLine("Argument 0 missing: LaunchMode (0: Cluster mode, 1: Worker mode)");
            }

            LaunchMode launchMode = (LaunchMode)int.Parse(args[0]);
            
            //CRAMode CRAMode = (CRAMode)int.Parse(args[1]);

            switch (launchMode)
            {
                case LaunchMode.ClusterSetup:
                    var craClientLibrary = new CRAClientLibrary(craDataProvider);
                    IOperatorGraphConfigurator graphConfigurator = new OperatorGraphConfigurator(new KubernetesDeploymentUtility(), craClientLibrary);
                    userGraphConfiguration.ConfigureGraph(graphConfigurator);
                    graphConfigurator.BuildGraph().Wait();
                    break;
                case LaunchMode.LocalWorker:
                    if (args.Length < 3 || args.Length > 4)
                    {
                        Console.WriteLine("Worker mode has 2 required (r) and 1 optional (o) arguments: instanceName (r), portNumber (r), ipAddress (o)");
                        return;
                    }
                    string instanceName = args[1];
                    int portNum = int.Parse(args[2]);
                    string ipAddress = args.Length == 4 ? args[3] : null;
                    Worker.Launch(instanceName, portNum, craDataProvider, ipAddress);
                    break;
                default:
                    Console.WriteLine("Invalid launch mode provided");
                    return;
            }
        }
    }

    public interface BlackSPConfiguration
    {
        void ConfigureGraph(IOperatorGraphConfigurator graphConfigurator);
    }
    public enum LaunchMode
    {
        ClusterSetup,
        LocalWorker
    }

    public enum CRAMode
    {
        Azure,
        File
    }

}
