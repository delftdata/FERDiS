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

        public Task<TMessage> Take(CancellationToken t) => TakeWithPriority(t);

        private Task<TMessage> TakeWithoutPriority(CancellationToken t)
        {
            var activeQueuePairs = _msgQueues.Where(kv => !_blockedConnections.Contains(kv.Key));

            TMessage message = default;
            
            int takenIndex = BlockingCollection<TMessage>.TakeFromAny(activeQueuePairs.Select(kv => kv.Value.UnderlyingCollection).ToArray(), out message, t);
            string takenConnectionKey = activeQueuePairs.ElementAt(takenIndex).Key;
            
            MessageOrigin = _originDictionary[takenConnectionKey];
            return Task.FromResult(message);
        }


        [Obsolete]
        private Task<TMessage> TakeWithPriority(CancellationToken t)
        {
            var activeQueuePairs = _msgQueues.Where(kv => !_blockedConnections.Contains(kv.Key));

            var priorityConnectionKeys = _vertexConfiguration.InputEndpoints.Where(ie => ie.IsBackchannel).SelectMany(ie => ie.GetAllConnectionKeys());
            var activePrioQueues = activeQueuePairs.Where(kv => priorityConnectionKeys.Contains(kv.Key));

            TMessage message = default;

            int takenIndex = -1;
            string takenConnectionKey = string.Empty;
            if (activePrioQueues.Select(q => q.Value.UnderlyingCollection).Any(queue => (queue.Count / (double)queue.BoundedCapacity) > 0.20d)) //any prio queue over 20% full? then take from those
            {
                takenIndex = BlockingCollection<TMessage>.TakeFromAny(activePrioQueues.Select(kv => kv.Value.UnderlyingCollection).ToArray(), out message, t);
                takenConnectionKey = activePrioQueues.ElementAt(takenIndex).Key;
            } 
            else //else, take from any input channel
            {
                takenIndex = BlockingCollection<TMessage>.TakeFromAny(activeQueuePairs.Select(kv => kv.Value.UnderlyingCollection).ToArray(), out message, t);
                takenConnectionKey = activeQueuePairs.ElementAt(takenIndex).Key;
            }

            MessageOrigin = _originDictionary[takenConnectionKey];
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
            var flushes = new List<Task>();
            foreach(var (endpoint, shardId) in _originDictionary.Values)
            {
                if(instanceNamesToFlush.Contains(endpoint.GetRemoteInstanceName(shardId)))
                {
                    flushes.Add(_msgQueues[endpoint.GetConnectionKey(shardId)].BeginFlush());
                }
            }
            if(flushes.Count != instanceNamesToFlush.Count())
            {
                throw new ArgumentException("Invalid instanceName in enumerable", nameof(instanceNamesToFlush));
            }

            _logger.Debug($"Receiver flushing {flushes.Count}/{_originDictionary.Count} queues");
            await Task.WhenAll(flushes).ConfigureAwait(false);
            _logger.Debug($"Receiver flushed {flushes.Count}/{_originDictionary.Count} queues");
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
                    //_msgQueues[connectionKey] = new BlockingFlushableQueue<TMessage>(config.IsBackchannel ? int.MaxValue : Constants.DefaultThreadBoundaryQueueSize);
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

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
