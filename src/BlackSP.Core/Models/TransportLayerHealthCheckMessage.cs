using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models
{
    public class TransportLayerHealthCheckMessage : IMessage
    {
        public bool IsControl => false;

        public int PartitionKey => 0;

        public string FromInstance { get; set; }

        public string ToInstance { get; set; }

        public void AddPayload<TPayload>(TPayload payload) where TPayload : MessagePayloadBase
        {
            throw new NotImplementedException();
        }

        public bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : MessagePayloadBase
        {
            throw new NotImplementedException();
        }
    }
}
