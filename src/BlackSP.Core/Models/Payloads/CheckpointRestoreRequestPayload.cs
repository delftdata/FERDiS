using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models.Payloads
{
    [ProtoContract]
    public class CheckpointRestoreRequestPayload : MessagePayloadBase
    {

        public static new string MetaDataKey => "control:checkpointrestore";

        /// <summary>
        /// Holds instancename-guid pairs indicating which checkpoint guid should be restored for each instance.
        /// Instances are expected to retrieve their respective value from the dictionary.<br/>
        /// Lastly, if there is no value for a given key, no checkpoint has to be restored
        /// </summary>
        [ProtoMember(1)]
        public Dictionary<string, Guid> InstanceCheckpointMap { get; set; }

    }
}
