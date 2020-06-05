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

        /// <summary>
        /// Returns a copy of the message with a new payload
        /// </summary>
        /// <param name="newPayload"></param>
        /// <returns></returns>
        IMessage Copy(IEvent newPayload);
    }
}
