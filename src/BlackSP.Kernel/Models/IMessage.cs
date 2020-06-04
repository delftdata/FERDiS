using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IMessage
    {
        IEvent Payload { get; }

        IDictionary<string, object> Metadata { get; }

        bool IsControl { get; }
    }
}
