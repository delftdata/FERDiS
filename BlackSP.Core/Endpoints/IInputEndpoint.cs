using BlackSP.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{
    public interface IInputEndpoint
    {

        /// <summary>
        /// Fetches deserialized event from input channel
        /// throws InvalidOperationException when channel is empty
        /// </summary>
        /// <returns></returns>
        IEvent GetNext();

        /// <summary>
        /// Check if input channel has any deserialized input ready
        /// </summary>
        /// <returns></returns>
        bool HasInput();

        void Ingress(Stream s, CancellationToken t);
    }
}
