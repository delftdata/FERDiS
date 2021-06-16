using BlackSP.CRA.Configuration;
using BlackSP.CRA.Utilities;
using BlackSP.CRA.Vertices;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Application;
using CRA.ClientLibrary;
using CRA.DataProvider;
using CRA.DataProvider.Azure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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


        public static void StartWorker(string[] args)
        {
            //TextWriterTraceListener myWriter = new TextWriterTraceListener(System.Console.Out);
            //Trace.Listeners.Add(myWriter);

            if (args.Length < 2)
            {
                Console.WriteLine("Worker for Common Runtime for Applications (CRA) [http://github.com/Microsoft/CRA]");
                Console.WriteLine("Usage: CRA.Worker.exe instancename (e.g., instance1) port (e.g., 11000) [ipaddress (null for default)] [secure_network_assembly_name secure_network_class_name]");
                return;
            }

            string ipAddress = GetLocalIPAddress();
            string storageConnectionString = null;
            IDataProvider dataProvider = null;
            int connectionsPoolPerWorker;
            string connectionsPoolPerWorkerString = null; 



            if (storageConnectionString == null)
            {
                storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING");
            }

            if (storageConnectionString != null)
            {
                dataProvider = new AzureDataProvider(storageConnectionString);
            }

            if (connectionsPoolPerWorkerString != null)
            {
                try
                {
                    connectionsPoolPerWorker = Convert.ToInt32(connectionsPoolPerWorkerString);
                }
                catch
                {
                    throw new InvalidOperationException("Maximum number of connections per CRA worker is wrong. Use appSettings in your app.config to provide this using the key CRA_WORKER_MAX_CONN_POOL.");
                }
            }
            else
            {
                connectionsPoolPerWorker = 1000;
            }

            

            var worker = new CRAWorker(
                args[0],
                ipAddress,
                Convert.ToInt32(args[1]),
                dataProvider,
                null,
                connectionsPoolPerWorker);

            worker.EnableParallelConnections();
            worker.DisableDynamicLoading();
            Console.WriteLine("Parallel connection enabled, dynamic loading disabled.");
            worker.SetTcpConnectionTimeout(5000);
            worker.SetConnectionRetryDelay(2500);
            //worker.SideloadVertex(new OperatorVertex(), args[2]);
            worker.Start();
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new InvalidOperationException("Local IP Address Not Found!");
        }
    }
}
