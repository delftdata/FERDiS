using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;

namespace BlackSP.Core.OperatorShells
{
    //- each operator pair will have their own endpoints connected --> not shared among operators
    //- operator can just enqueue outgoing events in all output queues
    //- endpoints will write to input or read from assigned output queue
    //      endpoints will respectively handle partitioning among shards etc
    public abstract class OperatorShellBase : IOperatorShell, IDisposable
    {
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private readonly IOperator _pluggedInOperator;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<IEvent> _inputQueue;

        private Task _operatingThread;

        /// <summary>
        /// Base constructor for Operators, will throw when passing null options
        /// </summary>
        /// <param name="options"></param>
        public OperatorShellBase(IOperator pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));

            _inputQueue = new BlockingCollection<IEvent>();
            _cancellationTokenSource = new CancellationTokenSource();
        }


        public abstract IEnumerable<IEvent> OperateOnEvent(IEvent @event);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _inputQueue?.Dispose();
                    _cancellationTokenSource?.Dispose();
                    _operatingThread?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
