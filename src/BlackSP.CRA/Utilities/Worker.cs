using CRA.ClientLibrary;
using CRA.DataProvider;
using System;
using System.Net;
using System.Net.Sockets;

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
        public static void Launch(string instanceName, int portNum, IDataProvider dataProvider, string ipAddress = null)
        {
            int connPoolSize = 1000;

            var worker = new CRAWorker(
                instanceName,
                ipAddress ?? GetLocalIPAddress(),
                portNum,
                dataProvider,
                null,
                connPoolSize);
            try
            {
                worker.Start();
            } 
            finally
            {
                worker.Dispose();
            }
            
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
