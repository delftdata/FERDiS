using BlackSP.Kernel.Events;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public class Message : IMessage
    {
        public IEvent Payload { get; private set; }

        public IDictionary<string, object> Metadata { get; private set; }

        public bool IsControl { get; private set; }
    }
}
