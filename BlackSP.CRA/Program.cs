using BlackSP.CRA.Vertices;
using CRA.ClientLibrary;
using System;
using System.Threading.Tasks;
using CRA.Worker;
using BlackSP.CRA.Utilities;

namespace BlackSP.CRA
{

    enum LaunchMode
    {
        ClusterSetup,
        LocalWorker
    }

    public class Program
    {
        /// <summary>
        /// Argument 0: launchMode (0 = cluster mode, 1 = worker mode)<br/>
        /// Argument 1: worker instance name (only in worker mode)<br/>
        /// Argument 2: worker communication port (only in worker mode)<br/>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Expecting launchmode integer (0: cluster configurator, 1: worker mode)");
                return;
            }

            LaunchMode mode = (LaunchMode)int.Parse(args[0]);
            switch(mode)
            {
                case LaunchMode.ClusterSetup:
                    await Cluster.Setup();
                    break;
                case LaunchMode.LocalWorker:
                    if(args.Length < 3 || args.Length > 4)
                    {
                        Console.WriteLine("Worker mode has 2 required (r) and 1 optional (o) arguments instanceName (r), portNumber (r), ipAddress (o)");
                        return;
                    }
                    string instanceName = args[1];
                    int portNum = int.Parse(args[2]);
                    string ipAddress = args.Length == 4 ? args[3] : null;
                    Worker.Launch(instanceName, portNum, ipAddress);
                    break;
                default:
                    Console.WriteLine("Invalid launch mode provided");
                    return;
            }

            
        }
    }
}
