using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control.Payloads
{
    [ProtoContract]
    public class LogPruneRequestPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "control:logprune";
        
        [ProtoMember(1)]
        public string InstanceName { get; set; }

        [ProtoMember(2)]
        public int SequenceNumber { get; set; }
    }
}
