using BlackSP.Kernel.Events;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public class Message : IMessage
    {
        public IEvent Payload { get; set; }

        public IDictionary<string, object> Metadata { get; private set; }

        public bool IsControl { get; private set; }

        public IMessage Copy(IEvent newPayload)
        {
            return new Message()
            {
                Payload = newPayload,
                Metadata = new Dictionary<string, object>(Metadata),
                IsControl = IsControl
            };
        }
    }
}
