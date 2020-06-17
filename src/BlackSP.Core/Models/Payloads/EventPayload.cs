using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models.Payloads
{
    [ProtoContract]
    public class EventPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:event";
        
        [ProtoMember(1)]
        public IEvent Event { get; set; } 
    }
}
