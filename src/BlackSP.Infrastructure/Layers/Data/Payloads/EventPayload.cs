using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data.Payloads
{
    [ProtoContract]
    [Serializable]
    public class EventPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:event";
        
        [ProtoMember(1)]
        public IEvent Event { get; set; } 
    }
}
