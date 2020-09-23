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
    /// Receives input from any source. Exposes received messages through the ISource interface.<br/>
    /// Sorts and orders input based on message types to be consumed one-by-one.
    /// </summary>
    public sealed class ReceiverMessageSource<TMessage> : IReceiver<TMessage>, ISource<TMessage>, IDisposable
        where TMessage : IMessage
    {
        public (IEndpointConfiguration, int) MessageOrigin { get; private set; }

        private readonly IVertexConfiguration _vertexConfiguration;

        private IDictionary<string, (IEndpointConfiguration, int)> _originDictionary;
        private IDictionary<string, BlockingCollection<TMessage>> _msgQueues;
        private List<string> _blockedConnections;

        private ReceptionFlags _receptionFlags;
        private object lockObj;
        private bool disposedValue;

        public ReceiverMessageSource(IVertexConfiguration vertexConfiguration)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            
            _msgQueues = new Dictionary<string, BlockingCollection<TMessage>>();
            _originDictionary = new Dictionary<string, (IEndpointConfiguration, int)>();
            InitialiseDataStructures();
            _blockedConnections = new List<string>();
            _receptionFlags = ReceptionFlags.Control | ReceptionFlags.Data; //TODO: even set flags on constuct?
            lockObj = new object();
        }

        public Task<TMessage> Take(CancellationToken t)
        {
            TMessage message = default;
            var activeQueuePairs = _msgQueues.Where(kv => !_blockedConnections.Contains(kv.Key));
            var activeQueues = activeQueuePairs.Select(kv => kv.Value).ToArray();
            var takenIndex = BlockingCollection<TMessage>.TakeFromAny(activeQueues, out message, t);
            var connectionKey = activeQueuePairs.Select(pair => pair.Key).ElementAt(takenIndex);
            MessageOrigin = _originDictionary[connectionKey];
            return Task.FromResult(message);
        }

        public BlockingCollection<TMessage> GetReceptionQueue(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);
            lock (lockObj)
            {
                return _msgQueues[connectionKey];
            }
        }

        public Task Flush()
        {
            lock (lockObj)
            {
                foreach(var oldQueue in _msgQueues.Values)
                {
                    oldQueue.CompleteAdding();
                    oldQueue.Dispose();
                }
                InitialiseDataStructures();
            }
            return Task.CompletedTask;
        }

        public void Block(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);
            //TODO: invalid operation exception
            _blockedConnections.Add(connectionKey);
        }

        public void Unblock(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);
            if(!_blockedConnections.Remove(connectionKey))
            {
                throw new InvalidOperationException("Cannot unblock a connection that is not blocked");
            }
        }

        public void SetFlags(ReceptionFlags mode)
        {
            _receptionFlags = mode;
        }

        public ReceptionFlags GetFlags()
        {
            return _receptionFlags;
        }

        private void InitialiseDataStructures()
        {
            foreach (var config in _vertexConfiguration.InputEndpoints)
            {
                for (int i = 0; i < config.RemoteInstanceNames.Count(); i++)
                {
                    int shardId = i;
                    var connectionKey = config.GetConnectionKey(shardId);
                    _msgQueues[connectionKey] = new BlockingCollection<TMessage>(Constants.DefaultThreadBoundaryQueueSize);
                    _originDictionary[connectionKey] = (config, shardId);
                }
            }
        }

        #region dispose pattern
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    foreach(var queue in _msgQueues.Values)
                    {
                        queue.Dispose();
                    }
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
