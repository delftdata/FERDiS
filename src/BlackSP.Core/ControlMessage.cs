using BlackSP.Kernel.Events;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public class ControlMessage : IMessage
    {
        public IEvent Payload { get; set; }

        public IDictionary<string, object> Metadata { get; private set; }

        public bool IsControl => true;

        public ControlMessage() { }

        public ControlMessage(IEvent payload)
        {
            Payload = payload; //payload is allowed to be null
            Metadata = new Dictionary<string, object>();
        }

        public IMessage Copy(IEvent newPayload)
        {
            return new ControlMessage()
            {
                Payload = newPayload,
                Metadata = new Dictionary<string, object>(Metadata),
            };
        }
    }
}
