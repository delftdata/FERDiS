using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data.Payloads
{
    [ProtoContract]
    public class BarrierPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:barrier";

    }
}
