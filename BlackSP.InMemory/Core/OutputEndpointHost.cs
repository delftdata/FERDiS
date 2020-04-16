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
            for(var shardId = 0; shardId < outgoingConnections.Length; shardId++)
            {
                Stream s = outgoingStreams[shardId];
                Connection c = outgoingConnections[shardId];
                Console.WriteLine($"{instanceName} - Starting output endpoint {endpointName}, shard {shardId}");
                _outputEndpoint.RegisterRemoteShard(c.ToShardId);
                _outputEndpoint.SetRemoteShardCount(c.ToShardCount);
                threads.Add(Task.Run(() => _outputEndpoint.Egress(s, c.ToShardId, token)));
            }

            await await Task.WhenAny(threads);
        }
    }
}
