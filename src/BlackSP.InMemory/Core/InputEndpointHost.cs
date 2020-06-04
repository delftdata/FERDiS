using BlackSP.InMemory.Configuration;
using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class InputEndpointHost
    {
        private readonly IInputEndpoint _inputEndpoint;
        private readonly ConnectionTable _connectionTable;

        public InputEndpointHost(IInputEndpoint inputEndpoint, ConnectionTable connectionTable)
        {
            _inputEndpoint = inputEndpoint ?? throw new ArgumentNullException(nameof(inputEndpoint));
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
        }

        /// <summary>
        /// Launches threads for each incoming connection
        /// </summary>
        /// <returns></returns>
        public async Task Start(string instanceName, string endpointName, CancellationToken token)
        {
            var incomingStreams = _connectionTable.GetIncomingStreams(instanceName, endpointName);
            var incomingConnections = _connectionTable.GetIncomingConnections(instanceName, endpointName);

            var threads = new List<Task>();
            for(var shardId = 0; shardId < incomingConnections.Length; shardId++)
            {
                Stream s = incomingStreams[shardId];
                Connection c = incomingConnections[shardId];
                Console.WriteLine($"{instanceName} - Starting input endpoint {endpointName}, shard {shardId}");
                threads.Add(Task.Run(() => _inputEndpoint.Ingress(s, c.FromEndpointName, c.FromShardId, token)));
            }

            await await Task.WhenAny(threads);
        }
    }
}
