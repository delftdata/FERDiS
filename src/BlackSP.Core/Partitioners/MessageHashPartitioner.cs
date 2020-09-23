using BlackSP.Core.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Partitioners
{
    /// <summary>
    /// Partitions using a simple modulo operation over the number of possible remote instances
    /// </summary>
    public class MessageHashPartitioner : IPartitioner<IMessage>
    {
        private readonly IVertexConfiguration _vertexConfiguration;

        public MessageHashPartitioner(IVertexConfiguration vertexConfiguration)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }

        public IEnumerable<string> Partition(IMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            var targetEndpoints = _vertexConfiguration.OutputEndpoints.Where(e => e.IsControl == message.IsControl);
            foreach(var endpoint in targetEndpoints)
            {
                if(message.PartitionKey.HasValue)
                {   //got partitionkey, so do partitioning
                    var targetShard = Math.Abs(message.PartitionKey.Value) % endpoint.RemoteInstanceNames.Count();
                    yield return endpoint.GetConnectionKey(targetShard);
                } 
                else
                {   //got no partitionkey, so do broadcast
                    foreach(var connectionKey in endpoint.GetAllConnectionKeys())
                    {
                        yield return connectionKey;
                    }
                }
                
            }
        }
    }
}
