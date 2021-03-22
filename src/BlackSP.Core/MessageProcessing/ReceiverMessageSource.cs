using BlackSP.Core.Exceptions;
using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BlackSP.Core.MessageProcessing
{
    /// <summary>
    /// Receives input from any source. Exposes received messages through the ISource interface.<br/>
    /// Sorts and orders input based on message types to be consumed one-by-one.
    /// </summary>
    public sealed class ReceiverMessageSource<TMessage> : IReceiverSource<TMessage>, IDisposable
        where TMessage : class, IMessage
    {
        public (IEndpointConfiguration, int) MessageOrigin { get; private set; }


        private readonly IObjectSerializer _serializer;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;
        
        /// <summary>
        /// dict mapping connection keys to origin pairs
        /// </summary>
        private IDictionary<string, (IEndpointConfiguration, int)> _originDictionary;

        /// <summary>
        /// dict mapping connection keys to flush completion sources
        /// </summary>
        private IDictionary<string, TaskCompletionSource<bool>> _flushDictionary;

        /// <summary>
        /// internal dict for block/unblock api
        /// </summary>
        private IDictionary<string, SemaphoreSlim> _blockDictionary;

        /// <summary>
        /// internal dict for blocking channels for priority management
        /// </summary>
        private IDictionary<string, SemaphoreSlim> _priorityAccessDictionary;

        /// <summary>
        /// internal dict for synchronization between Take and Receive calls
        /// </summary>
        private IDictionary<string, SemaphoreSlim> _takeBeforeDeliverDictionary;

        /// <summary>
        /// semaphore to allow a single producer to take priority
        /// </summary>
        private SemaphoreSlim _priorityAccess;

        /// <summary>
        /// semaphore to allow a single producer into the reception critical section
        /// </summary>
        private SemaphoreSlim _nextMessageAccess;


        private BufferBlock<(TMessage, IEndpointConfiguration, int)> _receivedMessages;


        private bool disposedValue;

        private (TMessage, IEndpointConfiguration, int) _lastTake { get; set; }

        public ReceiverMessageSource(IObjectSerializer serializer, IVertexConfiguration vertexConfiguration, ILogger logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _originDictionary = new Dictionary<string, (IEndpointConfiguration, int)>();
            _flushDictionary = new Dictionary<string, TaskCompletionSource<bool>>();
            
            _blockDictionary = new Dictionary<string, SemaphoreSlim>();
            _priorityAccessDictionary = new Dictionary<string, SemaphoreSlim>();
            _takeBeforeDeliverDictionary = new Dictionary<string, SemaphoreSlim>();
            _priorityAccess = new SemaphoreSlim(1, 1);
            _nextMessageAccess = new SemaphoreSlim(1, 1);

            _receivedMessages = new BufferBlock<(TMessage, IEndpointConfiguration, int)>(new DataflowBlockOptions { BoundedCapacity = 1 });

            InitialiseDataStructures();
        }

        public async Task<TMessage> Take(CancellationToken t)
        {
            var (lMsg, lOrigin, lShard) = _lastTake;
            if(lMsg != null)
            {
                _takeBeforeDeliverDictionary.Get(lOrigin.GetConnectionKey(lShard)).Release();
            }

            if (!await _receivedMessages.OutputAvailableAsync(t).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Internal reception block may not complete");
            }

            var (msg, origin, shard) = _lastTake = await _receivedMessages.ReceiveAsync(t).ConfigureAwait(false);

            MessageOrigin = (origin, shard);
            return msg;
        }
        
        public async Task Receive(byte[] message, IEndpointConfiguration origin, int shardId, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);

            //perform flush checks
            if(!FlushPreReceptionChecks(message, connectionKey)) {
                return;
            }
            //do deserialization
            var dserializedMsg = await _serializer.DeserializeAsync<TMessage>(message, t).ConfigureAwait(false);


            var accessed = new List<SemaphoreSlim>();

            var prioAccess = _priorityAccessDictionary.Get(connectionKey); 
            var blockAccess = _blockDictionary.Get(connectionKey); 
            try
            {
                await prioAccess.WaitAsync(t).ConfigureAwait(false); //ensure current channel is not being out-prioritized
                accessed.Add(prioAccess);
                await blockAccess.WaitAsync(t).ConfigureAwait(false); //ensure current channel is not blocked
                accessed.Add(blockAccess);
                //acquire access to the critical section..
                await _nextMessageAccess.WaitAsync(t).ConfigureAwait(false);
                accessed.Add(_nextMessageAccess);


                //use critical section
                await _receivedMessages.SendAsync((dserializedMsg, origin, shardId), t).ConfigureAwait(false);                
            }
            finally
            {
                accessed.ForEach(sema => sema.Release()); //allow next channel into the CS
            }

            await _takeBeforeDeliverDictionary[connectionKey].WaitAsync(t).ConfigureAwait(false); //wait for message to be taken before returning (prevent next delivery in case processing blocks channel)

        }

        public async Task TakePriority(IEndpointConfiguration prioOrigin, int shardId)
        {
            _ = prioOrigin ?? throw new ArgumentNullException(nameof(prioOrigin));
            var prioKey = prioOrigin.GetConnectionKey(shardId);
            var tasks = new List<Task>();

            await _priorityAccess.WaitAsync().ConfigureAwait(false);

            foreach(var (origin, shard) in _originDictionary.Values)
            {
                var key = origin.GetConnectionKey(shard);
                if(key == prioKey)
                {
                    continue;
                }
                tasks.Add(_priorityAccessDictionary.Get(key).WaitAsync());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public void ReleasePriority(IEndpointConfiguration prioOrigin, int shardId)
        {
            _ = prioOrigin ?? throw new ArgumentNullException(nameof(prioOrigin));
            var prioKey = prioOrigin.GetConnectionKey(shardId);
            foreach (var (origin, shard) in _originDictionary.Values)
            {
                var key = origin.GetConnectionKey(shard);
                if (key == prioKey)
                {
                    continue;
                }
                _priorityAccessDictionary.Get(key).Release();
            }
            _priorityAccess.Release();
        }

        public async Task Flush(IEnumerable<string> instanceNamesToFlush)
        {
            _ = instanceNamesToFlush ?? throw new ArgumentNullException(nameof(instanceNamesToFlush));
            var flushes = new List<Task>();
            foreach(var (endpoint, shardId) in _originDictionary.Values)
            {
                if(instanceNamesToFlush.Contains(endpoint.GetRemoteInstanceName(shardId)))
                {
                    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _flushDictionary.Add(endpoint.GetConnectionKey(shardId), tcs);
                    flushes.Add(tcs.Task);
                }
            }
            if(flushes.Count != instanceNamesToFlush.Count())
            {
                throw new ArgumentException($"Invalid instanceName in enumerable: {string.Join(", ", instanceNamesToFlush)}", nameof(instanceNamesToFlush));
            }

            if(flushes.Any())
            {
                _logger.Debug($"Receiver flushing {flushes.Count}/{_originDictionary.Count} queues");
                await Task.WhenAll(flushes).ConfigureAwait(false);
                _logger.Debug($"Receiver flushed {flushes.Count}/{_originDictionary.Count} queues");
            } 
            else
            {
                _logger.Debug($"Receiver did not have to flush any connections");
            }
            
        }

        public async Task Block(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            //wait associated semaphore
            var connectionKey = origin.GetConnectionKey(shardId);
            var semaphore = _blockDictionary[connectionKey];
            await semaphore.WaitAsync().ConfigureAwait(false);
        }

        public void Unblock(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            //release associated semaphore
            var connectionKey = origin.GetConnectionKey(shardId);
            var semaphore = _blockDictionary[connectionKey];
            semaphore.Release();
            
        }

        public void ThrowIfFlushInProgress(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            if (_flushDictionary.ContainsKey(origin.GetConnectionKey(shardId))) //check if flush in progress
            {
                throw new FlushInProgressException();
            }
        }

        /// <summary>
        /// performs flush checks
        /// </summary>
        /// <param name="message"></param>
        /// <param name="connectionKey"></param>
        /// <returns>wether message can be further processed</returns>
        private bool FlushPreReceptionChecks(byte[] message, string connectionKey)
        {
            if (!_flushDictionary.ContainsKey(connectionKey)) //check if flush in progress
            {
                return true;
            }

            if (message.IsFlushMessage()) //flush message returned, complete flush
            {
                _logger.Debug($"Setting flush result on {connectionKey}");
                _flushDictionary.Get(connectionKey).SetResult(true);
                _flushDictionary.Remove(connectionKey);
                return false;
            }
            else //regular message reception during flush, throw
            {
                _logger.Debug($"Throwing flush in progress exception on {connectionKey}");
                throw new FlushInProgressException();
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
                    _originDictionary[connectionKey] = (config, shardId);
                    _blockDictionary[connectionKey] = new SemaphoreSlim(1, 1);
                    _priorityAccessDictionary[connectionKey] = new SemaphoreSlim(1, 1);
                    _takeBeforeDeliverDictionary[connectionKey] = new SemaphoreSlim(0, 1);
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
