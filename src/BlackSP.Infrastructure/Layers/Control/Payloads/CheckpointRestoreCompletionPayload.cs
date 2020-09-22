using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control.Payloads
{
    [ProtoContract]
    public class CheckpointRestoreCompletionPayload : MessagePayloadBase
    {

        public static new string MetaDataKey => "control:checkpointrestorecompletion";

        /// <summary>
        /// InstanceName that restored a checkpoint
        /// </summary>
        [ProtoMember(1)]
        public string InstanceName { get; set; }

        /// <summary>
        /// CheckpointId that was restored by InstanceName.
        /// </summary>
        [ProtoMember(2)]
        public Guid CheckpointId { get; set; }

    }
}
