using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Data.Payloads
{
    [ProtoContract]
    [Serializable]
    public class CheckpointDependencyPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:checkpointdependency";


        [ProtoMember(1)]
        public Guid CheckpointId { get; set; }

    }
}
