using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface ISource<T>
    {
        /// <summary>
        /// Flush the underlying message source
        /// </summary>
        /// <returns></returns>
        Task Flush();

        /// <summary>
        /// Take the next element from the source
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<T> Take(CancellationToken t);

        /// <summary>
        /// Reference information on the last object returned by the Take(..) method.
        /// </summary>
        (IEndpointConfiguration, int) MessageOrigin { get; }
}
}
