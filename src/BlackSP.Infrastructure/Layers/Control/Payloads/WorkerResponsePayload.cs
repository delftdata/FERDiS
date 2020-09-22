using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control.Payloads
{
    [ProtoContract]
    public class WorkerResponsePayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "worker:response";

        [ProtoMember(1)]
        public string OriginInstanceName { get; set; }

        [ProtoMember(2)]
        public bool UpstreamFullyConnected { get; set; }

        [ProtoMember(3)]
        public bool DownstreamFullyConnected { get; set; }

        [ProtoMember(4)]
        public bool DataProcessActive { get; set; }

        [ProtoMember(5)]
        public WorkerRequestType OriginalRequestType { get; set; }
    }
}
