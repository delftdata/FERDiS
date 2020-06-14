using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models
{
    [ProtoContract]
    public class ControlMessage : IMessage
    {
        public bool IsControl => true;

        //[ProtoMember(1)]
        public int PartitionKey => 0; //TODO: consider how to handle partitioning from coordinator?

        //[ProtoMember(1)]
        //public IDictionary<string, object> Metadata { get; private set; } 
        //TODO: protobuf will not like object..

        [ProtoMember(1)]
        public ControlMessageType Type { get; private set; }

        public ControlMessage()
        {
            //Metadata = new Dictionary<string, object>();
        }

        public ControlMessage Copy()
        {
            return new ControlMessage()
            {
                //Metadata = new Dictionary<string, object>(Metadata),
                Type = Type
            };
        }
    }

    public enum ControlMessageType
    {
        Heartbeat,
        StartDataProcess,
        CheckpointRestore
    }
}
