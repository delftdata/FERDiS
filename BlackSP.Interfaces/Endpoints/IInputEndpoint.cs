using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Interfaces.Endpoints
{
    public interface IInputEndpoint
    {
        /// <summary>
        /// Starts background threads that read messages from the stream
        /// and serializes them before enqueueing the results in the 
        /// inputqueue of linked operator
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task Ingress(Stream s, CancellationToken t);
    }
}
