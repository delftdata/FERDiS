using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data
{
    [ProtoContract]
    public sealed class DataMessage : MessageBase
    {
        public override bool IsControl => false;

        [ProtoMember(1)]
        public override IDictionary<string, MessagePayloadBase> MetaData { get; }

        [ProtoMember(2)]
        public override int? PartitionKey { get; }

        public DataMessage()
        {
            MetaData = new Dictionary<string, MessagePayloadBase>();
            PartitionKey = null;
        }

        public DataMessage(int? partitionKey)
        {
            MetaData = new Dictionary<string, MessagePayloadBase>();
            PartitionKey = partitionKey;
        }

        public DataMessage(IDictionary<string, MessagePayloadBase> metaData, int? partitionKey)
        {
            MetaData = new Dictionary<string, MessagePayloadBase>(metaData);
            PartitionKey = partitionKey;
        }
    }
}
