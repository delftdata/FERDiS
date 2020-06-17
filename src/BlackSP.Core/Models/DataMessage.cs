using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models
{
    [ProtoContract]
    public sealed class DataMessage : MessageBase
    {
        public override bool IsControl => false;

        public override int PartitionKey { get
            {
                if(this.TryGetPayload<EventPayload>(out var payload))
                {
                    return payload.Event.GetPartitionKey();
                }
                throw new InvalidOperationException("Missing event payload, cannot get partition key");
            }
        }

        [ProtoMember(1)]
        public override IDictionary<string, MessagePayloadBase> MetaData { get; }

        public DataMessage()
        {
            MetaData = new Dictionary<string, MessagePayloadBase>();
        }

        public DataMessage(IDictionary<string, MessagePayloadBase> metaData)
        {
            MetaData = new Dictionary<string, MessagePayloadBase>(metaData);
        }
    }
}
