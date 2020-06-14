using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models
{

    [ProtoContract]
    public class DataMessage : IMessage
    {
        [ProtoMember(1)]
        public IEvent Payload { get; set; }

        //public IDictionary<string, object> Metadata { get; private set; }

        public bool IsControl => false;

        public int PartitionKey => Payload.GetPartitionKey();

        public DataMessage() 
        {
            //Metadata = new Dictionary<string, object>();
        }

        public DataMessage(IEvent payload)
        {
            Payload = payload; //payload is allowed to be null
            //Metadata = new Dictionary<string, object>();
        }

        public DataMessage Copy(IEvent newPayload)
        {
            return new DataMessage()
            {
                Payload = newPayload,
                //Metadata = new Dictionary<string, object>(Metadata),
            };
        }
    }
}
