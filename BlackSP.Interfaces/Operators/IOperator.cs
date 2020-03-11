using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Starts the operating background thread<br/> 
        /// Returns said thread as Task
        /// </summary>
        Task Start();
        
        /// <summary>
        /// Stops the operating background thread<br/>
        /// Returns the background thread as Task to allow waiting for it to exit.
        /// </summary>
        Task Stop();
        
        void RegisterOutputEndpoint(IOutputEndpoint outputEndpoint);
    }
}
