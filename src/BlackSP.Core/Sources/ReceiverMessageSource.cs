using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
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
        private readonly ILogger _logger;
        private IDictionary<string, (IEndpointConfiguration, int)> _originDictionary;
        private IDictionary<string, BlockingFlushableQueue<TMessage>> _msgQueues;
        private List<string> _blockedConnections;

        private readonly object lockObj;
        private bool disposedValue;

        public ReceiverMessageSource(IVertexConfiguration vertexConfiguration, ILogger logger)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _msgQueues = new Dictionary<string, BlockingFlushableQueue<TMessage>>();
            _originDictionary = new Dictionary<string, (IEndpointConfiguration, int)>();
            InitialiseDataStructures();
            _blockedConnections = new List<string>();
            lockObj = new object();
        }

        public Task<TMessage> Take(CancellationToken t)
        {
            TMessage message = default;
            var activeQueuePairs = _msgQueues.Where(kv => !_blockedConnections.Contains(kv.Key));
            var activeQueues = activeQueuePairs.Select(kv => kv.Value.UnderlyingCollection).ToArray();
            var takenIndex = BlockingCollection<TMessage>.TakeFromAny(activeQueues, out message, t);
            var connectionKey = activeQueuePairs.Select(pair => pair.Key).ElementAt(takenIndex);
            MessageOrigin = _originDictionary[connectionKey];
            return Task.FromResult(message);
        }

        public IFlushableQueue<TMessage> GetReceptionQueue(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);
            lock (lockObj)
            {
                return _msgQueues[connectionKey];
            }
        }

        public async Task Flush(IEnumerable<string> instanceNamesToFlush)
        {
            _ = instanceNamesToFlush ?? throw new ArgumentNullException(nameof(instanceNamesToFlush));
            List<Task> flushes = new List<Task>();
            foreach(var (endpoint, shardId) in _originDictionary.Values)
            {
                if(instanceNamesToFlush.Contains(endpoint.RemoteInstanceNames.ElementAt(shardId)))
                {
                    flushes.Add(_msgQueues[endpoint.GetConnectionKey(shardId)].BeginFlush());
                }
            }
            _logger.Debug($"Started flushing {flushes.Count}/{instanceNamesToFlush.Count()} input endpoints");
            await Task.WhenAll(flushes).ConfigureAwait(false);
            _logger.Debug($"Completed flushing {flushes.Count}/{instanceNamesToFlush.Count()} input endpoints");
        }

        public void Block(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);
            if (_blockedConnections.Contains(connectionKey))
            {
                throw new InvalidOperationException("Cannot block a connection that is already blocked");
            }
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

        private void InitialiseDataStructures()
        {
            foreach (var config in _vertexConfiguration.InputEndpoints)
            {
                for (int i = 0; i < config.RemoteInstanceNames.Count(); i++)
                {
                    int shardId = i;
                    var connectionKey = config.GetConnectionKey(shardId);
                    _msgQueues[connectionKey] = new BlockingFlushableQueue<TMessage>(Constants.DefaultThreadBoundaryQueueSize);
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
