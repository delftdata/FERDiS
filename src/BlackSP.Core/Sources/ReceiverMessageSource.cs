using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Sources
{
    /// <summary>
    /// Receives input from any source. Exposes received messages through the IMessageSource interface.<br/>
    /// Sorts and orders input based on message types to be consumed one-by-one.
    /// </summary>
    public sealed class ReceiverMessageSource<TMessage> : IReceiver<TMessage>, ISource<TMessage>, IDisposable
        where TMessage : IMessage
    {
        private BlockingCollection<TMessage> _msgQueue;
        private ReceptionFlags _receptionFlags;
        private bool disposedValue;

        public ReceiverMessageSource()
        {
            _msgQueue = new BlockingCollection<TMessage>(Constants.DefaultThreadBoundaryQueueSize);
            _receptionFlags = ReceptionFlags.Control | ReceptionFlags.Data; //TODO: even set flags on constuct?
        }

        public TMessage Take(CancellationToken t)
        {
            while(_msgQueue.IsAddingCompleted)
            {
                Task.Delay(10, t).Wait();
            }
            return _msgQueue.Take(t);
        }

        public Task Flush()
        {
            _msgQueue.CompleteAdding();
            _msgQueue.Dispose();
            _msgQueue = new BlockingCollection<TMessage>(Constants.DefaultThreadBoundaryQueueSize);
            return Task.CompletedTask;
        }

        public void Receive(TMessage message, IEndpointConfiguration origin, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            
            _msgQueue.Add(message, t);
        }

        public void SetFlags(ReceptionFlags mode)
        {
            _receptionFlags = mode;
        }

        public ReceptionFlags GetFlags()
        {
            return _receptionFlags;
        }

        #region dispose pattern
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _msgQueue.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ReceiverMessageSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
