using BlackSP.Core.Models;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control
{
    [ProtoContract]
    public sealed class ControlMessage : MessageBase
    {
        public override bool IsControl => true;

        [ProtoMember(1)]
        public override int? PartitionKey { get; }

        [ProtoMember(2)]
        public override IDictionary<string, MessagePayloadBase> MetaData { get; }
        
        //not a protomember as its not really relevant to transfer between instances for this message type
        public override DateTime CreatedAtUtc { get; set; }

        public ControlMessage()
        {
            PartitionKey = null;
            MetaData = new Dictionary<string, MessagePayloadBase>();
            CreatedAtUtc = DateTime.MinValue;
        }

        public ControlMessage(int partitionKey)
        {
            PartitionKey = partitionKey;
            MetaData = new Dictionary<string, MessagePayloadBase>();
        }

        public ControlMessage(IDictionary<string, MessagePayloadBase> metaData, int? partitionKey = null)
        {
            PartitionKey = partitionKey;
            MetaData = new Dictionary<string, MessagePayloadBase>(metaData);
        }
    }
}
