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

        public IEnumerable<string> Partition(TMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            return !message.PartitionKey.HasValue
                ? _vertexConfiguration.OutputEndpoints.Where(e => e.IsControl == message.IsControl).SelectMany(e => e.GetAllConnectionKeys())
                : _vertexConfiguration.GetConnectionKeyByPartitionKey(message.PartitionKey.Value).Yield();
        }
    }
}
