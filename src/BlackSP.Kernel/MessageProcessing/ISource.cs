using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{

    public interface ISource
    {
        /// <summary>
        /// Reference information on the last object returned by the Take(..) method.
        /// </summary>
        (IEndpointConfiguration, int) MessageOrigin { get; }

        /// <summary>
        /// Begin flushing the message source
        /// </summary>
        /// <returns></returns>
        Task Flush(IEnumerable<string> upstreamInstancesToFlush);
    }

    public interface ISource<T> : ISource
    {
        

        /// <summary>
        /// Take the next element from the source
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<T> Take(CancellationToken t);

    }
}
