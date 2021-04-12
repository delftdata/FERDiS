using BlackSP.Core.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.MessageProcessing
{

    /// <summary>
    /// Partitions messages through special partition keys to target specific instances
    /// </summary>
    public class MessageTargetingPartitioner<TMessage> : IPartitioner<TMessage>
        where TMessage : class, IMessage
    {

        private readonly IVertexConfiguration _vertexConfiguration;

        public MessageTargetingPartitioner(IVertexConfiguration vertexConfiguration)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }

        public IEnumerable<(IEndpointConfiguration, int)> Partition(TMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if(!message.PartitionKey.HasValue) //no key, do broadcast
            { 
                foreach(var endpoint in _vertexConfiguration.OutputEndpoints.Where(e => e.IsControl == message.IsControl))
                {
                    int i = 0;
                    foreach(var _ in endpoint.RemoteInstanceNames)
                    {
                        yield return (endpoint, i);
                        i++;
                    }
                }
            }
            else
            {
                yield return _vertexConfiguration.GetTargetPairByPartitionKey(message.PartitionKey.Value);
            }
        }
    }
}
