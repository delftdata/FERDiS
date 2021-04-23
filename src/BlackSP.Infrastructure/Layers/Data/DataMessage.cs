using BlackSP.Core.Models;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data
{
    [ProtoContract]
    [Serializable]
    public sealed class DataMessage : MessageBase
    {
        public override bool IsControl => false;

        [ProtoMember(1)]
        public override IDictionary<string, MessagePayloadBase> MetaData { get; }

        [ProtoMember(2)]
        public override int? PartitionKey { get; }

        [ProtoMember(3)]
        public override DateTime CreatedAtUtc { get; set; }

        public DataMessage()
        {
            MetaData = new Dictionary<string, MessagePayloadBase>();
            PartitionKey = null;
            //CreatedAtUtc = DateTime.MinValue;
        }

        public DataMessage(DateTime createdAtUtc, int? partitionKey)
        {
            MetaData = new Dictionary<string, MessagePayloadBase>();
            PartitionKey = partitionKey;
            CreatedAtUtc = createdAtUtc;
        }

        public DataMessage(DateTime createdAtUtc, IDictionary<string, MessagePayloadBase> metaData, int? partitionKey)
        {
            MetaData = new Dictionary<string, MessagePayloadBase>(metaData);
            PartitionKey = partitionKey;
            CreatedAtUtc = createdAtUtc;
        }

        /// <summary>
        /// Copy-constructor
        /// </summary>
        /// <param name="message"></param>
        public DataMessage(DataMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            MetaData = new Dictionary<string, MessagePayloadBase>(message.MetaData);
            PartitionKey = message.PartitionKey;
            CreatedAtUtc = message.CreatedAtUtc;
            TargetOverride = message.TargetOverride;
        }
    }
}
