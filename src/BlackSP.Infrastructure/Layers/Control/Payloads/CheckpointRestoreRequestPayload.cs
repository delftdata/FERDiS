﻿using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Control.Payloads
{
    [ProtoContract]
    public class CheckpointRestoreRequestPayload : MessagePayloadBase
    {

        public static new string MetaDataKey => "control:checkpointrestore";


        /// <summary>
        /// Checkpoint identifier requested to restore
        /// </summary>
        [ProtoMember(1)]
        public Guid CheckpointId { get; set; }

        public CheckpointRestoreRequestPayload()
        {

        }

        public CheckpointRestoreRequestPayload(Guid checkpointId)
        {
            CheckpointId = checkpointId;
        }

    }
}
