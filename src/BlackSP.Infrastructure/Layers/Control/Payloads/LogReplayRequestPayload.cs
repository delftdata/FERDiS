using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control.Payloads
{
    [ProtoContract]
    public class LogReplayRequestPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "control:logreplay";
        
        [ProtoMember(1)]
        public IDictionary<string, int> ReplayMap { get; set; }

    }
}
