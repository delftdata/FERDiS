using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control.Payloads
{

    [ProtoContract]
    public class CheckpointTakenPayload : MessagePayloadBase
    {

        public static new string MetaDataKey => "control:checkpoint-taken";

        [ProtoMember(1)]
        public Guid CheckpointId { get; set; }

        [ProtoMember(2)]
        public IDictionary<string, int> AssociatedSequenceNumbers { get; set; }
    }
}
