using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core
{
    public class MessagePartitioner : IPartitioner
    {
        private readonly IVertexConfiguration _vertexConfiguration;

        public MessagePartitioner(IVertexConfiguration vertexConfiguration)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }

        public IEnumerable<string> Partition(IMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            //_ = message.Payload ?? throw new ArgumentNullException($"{nameof(message)} cannot have null {nameof(message.Payload)}");
            var targetEndpoints = _vertexConfiguration.OutputEndpoints.Where(e => e.IsControl == message.IsControl);
            foreach(var endpoint in targetEndpoints)
            {
                var targetShard = message.PartitionKey % endpoint.RemoteShardCount;
                yield return GetEndpointKey(endpoint.RemoteEndpointName, targetShard);
            }
        }

        public string GetEndpointKey(string endpointName, int shardId)
        {
            return $"{endpointName}{shardId}";
        }
    }
}
