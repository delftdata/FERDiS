using BlackSP.CRA.Vertices;
using CRA.ClientLibrary;
using System;
using System.Threading.Tasks;
using CRA.Worker;
using BlackSP.CRA.Utilities;
using CRA.DataProvider;
using CRA.DataProvider.Azure;
using CRA.DataProvider.File;

namespace BlackSP.CRA
{

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

    public class Program
    {
        /// <summary>
        /// Argument 0: launchMode (0 = cluster mode, 1 = worker mode)<br/>
        /// Argument 1: CRAMode (0 = Azure mode, 1 = FS mode)<br/>
        /// Argument 2: worker instance name (only in worker mode)<br/>
        /// Argument 3: worker communication port (only in worker mode)<br/>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            if(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING") == null)
            {
                Environment.SetEnvironmentVariable("AZURE_STORAGE_CONN_STRING", "DefaultEndpointsProtocol=https;AccountName=edgeandvertexstore;AccountKey=XN8F1mII7l0d0eLXaUGLKCxpzeAIt+VUtW4PxF+q3RRsuHf4kQ8FMRCpDSU1YvbVlgjtycl7pBk+Cb0iE6LvOw==;EndpointSuffix=core.windows.net");
            }

            if (args.Length < 1)
            {
                Console.WriteLine("Argument 0 missing: LaunchMode (0: Cluster mode, 1: Worker mode)");
            }

            if(args.Length < 2)
            {
                Console.WriteLine("Expecting CRAMode argument (0: Azure mode, 1: FS mode)");
                return;
            }


            LaunchMode launchMode = (LaunchMode)int.Parse(args[0]);
            CRAMode CRAMode = (CRAMode)int.Parse(args[1]);

            IDataProvider dataProvider = ConstructDataProvider(CRAMode);

            switch (launchMode)
            {
                case LaunchMode.ClusterSetup:
                    await Cluster.Setup(dataProvider);
                    break;
                case LaunchMode.LocalWorker:
                    if(args.Length < 4 || args.Length > 5)
                    {
                        Console.WriteLine("Worker mode has 2 required (r) and 1 optional (o) arguments instanceName (r), portNumber (r), ipAddress (o)");
                        return;
                    }
                    string instanceName = args[2];
                    int portNum = int.Parse(args[3]);
                    string ipAddress = args.Length == 5 ? args[4] : null;
                    Worker.Launch(instanceName, portNum, dataProvider, ipAddress);
                    break;
                default:
                    Console.WriteLine("Invalid launch mode provided");
                    return;
            }
        }

        private static IDataProvider ConstructDataProvider(CRAMode mode)
        {
            switch(mode)
            {
                case CRAMode.Azure:
                    return new AzureDataProvider(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING"));
                case CRAMode.File:
                    return new FileDataProvider();
                default:
                    throw new ArgumentOutOfRangeException("Invalid CRAMode provided");
            }
        }
    }
}
