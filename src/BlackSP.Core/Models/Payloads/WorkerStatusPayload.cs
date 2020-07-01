using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models.Payloads
{
    [ProtoContract]
    public class WorkerStatusPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "worker:status";

        [ProtoMember(1)]
        public string OriginInstanceName { get; set; }

        [ProtoMember(2)]
        public bool UpstreamFullyConnected { get; set; }

        [ProtoMember(3)]
        public bool DownstreamFullyConnected { get; set; }

        [ProtoMember(4)]
        public bool DataProcessActive { get; set; }
    }
}
