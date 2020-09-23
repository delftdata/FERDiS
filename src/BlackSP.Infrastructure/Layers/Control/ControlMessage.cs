using BlackSP.Core.Models;
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

        public ControlMessage()
        {
            PartitionKey = null;
            MetaData = new Dictionary<string, MessagePayloadBase>();
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
