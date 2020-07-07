using BlackSP.InMemory.Configuration;
using BlackSP.Kernel.Endpoints;
using Nerdbank.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class OutputEndpointHost
    {
        private readonly IOutputEndpoint _outputEndpoint;
        private readonly ConnectionTable _connectionTable;

        public OutputEndpointHost(IOutputEndpoint outputEndpoint, ConnectionTable connectionTable)
        {
            _outputEndpoint = outputEndpoint ?? throw new ArgumentNullException(nameof(outputEndpoint));
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
        }

        /// <summary>
        /// Launches threads for each incoming connection
        /// </summary>
        /// <returns></returns>
        public async Task Start(string instanceName, string endpointName, CancellationToken token)
        {
            var outgoingStreams = _connectionTable.GetOutgoingStreams(instanceName, endpointName);
            var outgoingConnections = _connectionTable.GetOutgoingConnections(instanceName, endpointName);

            var threads = new List<Task>();
            for(var i = 0; i < outgoingConnections.Length; i++)
            {
                int shardId = i;
                Stream s = outgoingStreams[shardId];
                Connection c = outgoingConnections[shardId];
                threads.Add(Task.Run(() => EgressWithRestart(instanceName, endpointName, shardId, 3, TimeSpan.FromSeconds(10), token)));
            }

            try
            {
                await await Task.WhenAny(threads);
            }
            catch (OperationCanceledException)
            {
                foreach (var stream in outgoingStreams)
                {
                    stream.Close();
                    (stream as SimplexStream)?.CompleteWriting();
                    stream.Dispose(); //force close the stream to trigger exception in output endpoint as if it were a dropped network stream
                }
                foreach (var connection in outgoingConnections)
                {
                    _connectionTable.RegisterConnection(connection); //re-register connection to create new streams around a failed instance
                }
            }
        }

        private async Task EgressWithRestart(string instanceName, string endpointName, int shardId, int maxRestarts, TimeSpan restartTimeout, CancellationToken token)
        {
            while (true)
            {
                Stream s = null;
                Connection c = null;
                try
                {
                    s = _connectionTable.GetOutgoingStreams(instanceName, endpointName)[shardId];
                    c = _connectionTable.GetOutgoingConnections(instanceName, endpointName)[shardId];
                    
                    Console.WriteLine($"{c.FromInstanceName} - Output endpoint {c.FromEndpointName} starting.\t(to {c.ToInstanceName}${c.ToEndpointName}${c.ToShardId})");
                    await _outputEndpoint.Egress(s, c.ToEndpointName, c.ToShardId, token);
                    Console.WriteLine($"{c.FromInstanceName} - Output endpoint {c.FromEndpointName} exited without exceptions");
                    
                    return;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"{c.FromInstanceName} - Output endpoint {c.FromEndpointName} exiting due to cancellation");
                    throw;
                }
                catch (Exception)
                {
                    if (maxRestarts-- == 0)
                    {
                        Console.WriteLine($"{c.FromInstanceName} - Output endpoint {c.FromEndpointName} exited with exceptions, no restart: exceeded maxRestarts.");
                        throw;
                    }
                    Console.WriteLine($"{c.FromInstanceName} - Output endpoint {c.FromEndpointName} exited with exceptions, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout);
                }
            }
        }
    }
}
