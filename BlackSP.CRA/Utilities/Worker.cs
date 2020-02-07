using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using CRA.DataProvider;
using CRA.DataProvider.Azure;

namespace BlackSP.CRA.Utilities
{
    /// <summary>
    /// Public utility class to spawn a local CRA worker<br/>
    /// Primarily useful for debug purposes
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Expects two arguments: instanceName & port
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static void Launch(string instanceName, int portNum, string ipAddress = null)
        {
            int connPoolSize = 1000;
            
            IDataProvider dataProvider = new AzureDataProvider(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING"));
            
            var worker = new CRAWorker(
                instanceName,
                ipAddress ?? GetLocalIPAddress(),
                portNum,
                dataProvider,
                null,
                connPoolSize);

            worker.Start();
        }

        //TODO: resolve correct ip? just got dockernat ip :/
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
