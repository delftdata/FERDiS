using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data.Payloads
{
    /// <summary>
    /// Message payload that carries information about checkpoints in other instances
    /// </summary>
    [ProtoContract]
    public class CICPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:cic";
    }
}
