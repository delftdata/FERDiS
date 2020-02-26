using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BlackSP.Interfaces.Operators
{
    public interface IOperator
    {
        /// <summary>
        /// Handle for input endpoints to place events in operator queue
        /// </summary>
        BlockingCollection<IEvent> InputQueue { get; }

        /// <summary>
        /// Public cancellation token for processes that can observe the operator
        /// and want to be notified of the operator cancelling its operations
        /// </summary>
        CancellationToken CancellationToken { get; }
        void Start();
        void Stop();
        void RegisterOutputEndpoint(IOutputEndpoint outputEndpoint);
    }
}
