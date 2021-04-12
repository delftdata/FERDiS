using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data.Payloads
{

    [ProtoContract]
    public class SequenceNumberPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:sequence-number";

        [ProtoMember(1)]
        public int SequenceNumber { get; set; }

    }
}
