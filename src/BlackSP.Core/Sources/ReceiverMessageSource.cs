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

namespace BlackSP.Core.Sources
{
    /// <summary>
    /// Receives input from any source. Exposes received messages through the ISource interface.<br/>
    /// Sorts and orders input based on message types to be consumed one-by-one.
    /// </summary>
    public sealed class ReceiverMessageSource<TMessage> : IReceiver<TMessage>, ISource<TMessage>, IDisposable
        where TMessage : class, IMessage
    {
        public (IEndpointConfiguration, int) MessageOrigin { get; private set; }

        private readonly IObjectSerializer _serializer;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;
        
        private IDictionary<string, (IEndpointConfiguration, int)> _originDictionary;
        private IDictionary<string, TaskCompletionSource<bool>> _flushDictionary;
        private IDictionary<string, SemaphoreSlim> _accessDictionary;

        private SemaphoreSlim _channelAccess;
        private SemaphoreSlim _priorityAccess;

        /// <summary>
        /// Contains (a) received message(s) ready for delivery
        /// </summary>
        private Channel<(TMessage, IEndpointConfiguration, int)> _channel;

        //private List<string> _blockedConnections;

        private readonly object lockObj;
        private bool disposedValue;

        public ReceiverMessageSource(IObjectSerializer serializer, IVertexConfiguration vertexConfiguration, ILogger logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _originDictionary = new Dictionary<string, (IEndpointConfiguration, int)>();
            _flushDictionary = new Dictionary<string, TaskCompletionSource<bool>>();
            _accessDictionary = new Dictionary<string, SemaphoreSlim>();
            _channelAccess = new SemaphoreSlim(1, 1);
            _priorityAccess = new SemaphoreSlim(1, 1);
            InitialiseDataStructures();
            
            lockObj = new object();

            //note single capacity
            _channel = Channel.CreateBounded<(TMessage, IEndpointConfiguration, int)>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.Wait });
        }

        public async Task<TMessage> Take(CancellationToken t)
        {
            if(await _channel.Reader.WaitToReadAsync(t))
            {
                var (msg, origin, shard) = await _channel.Reader.ReadAsync(t);
                MessageOrigin = (origin, shard);
                return msg;
            } 
            else
            {
                throw new InvalidOperationException($"{nameof(ReceiverMessageSource<TMessage>)} internal channel was completed, this yields an invalid program state");
            }
            
        }

#if false
        private Task<TMessage> TakeWithoutPriority(CancellationToken t)
        {
            var activeQueuePairs = _msgQueues.Where(kv => !_blockedConnections.Contains(kv.Key));

            TMessage message = default;
            
            int takenIndex = BlockingCollection<TMessage>.TakeFromAny(activeQueuePairs.Select(kv => kv.Value.UnderlyingCollection).ToArray(), out message, t);
            string takenConnectionKey = activeQueuePairs.ElementAt(takenIndex).Key;
            
            MessageOrigin = _originDictionary[takenConnectionKey];
            return Task.FromResult(message);
        }


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
#endif
        
        
        public async Task Receive(byte[] message, IEndpointConfiguration origin, int shardId, CancellationToken t)
        {

            _ = message ?? throw new ArgumentNullException(nameof(message));
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            var connectionKey = origin.GetConnectionKey(shardId);

            //perform flush checks
            FlushPreReceptionChecks(message, connectionKey);

            //do deserialization
            var dserializedMsg = await _serializer.DeserializeAsync<TMessage>(message, t).ConfigureAwait(false);

            var personalAccess = _accessDictionary.Get(connectionKey);
            try
            {            
                //gain access to critical section
                await personalAccess.WaitAsync().ConfigureAwait(false);
                await _channelAccess.WaitAsync().ConfigureAwait(false);
                
                //use critical section
                if (await _channel.Writer.WaitToWriteAsync(t))
                {
                    await _channel.Writer.WriteAsync((dserializedMsg, origin, shardId), t);
                }
                else
                {
                    throw new InvalidOperationException("Tried to write to closed channel");
                }
            }
            finally
            {
                //release access to critical section
                _channelAccess.Release();
                personalAccess.Release();
            }
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
                tasks.Add(_accessDictionary.Get(key).WaitAsync());
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
                _accessDictionary.Get(key).Release();
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
                throw new ArgumentException("Invalid instanceName in enumerable", nameof(instanceNamesToFlush));
            }

            _logger.Debug($"Receiver flushing {flushes.Count}/{_originDictionary.Count} queues");
            await Task.WhenAll(flushes).ConfigureAwait(false);
            _logger.Debug($"Receiver flushed {flushes.Count}/{_originDictionary.Count} queues");
        }

        public async Task Block(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            //wait associated semaphore
            var connectionKey = origin.GetConnectionKey(shardId);
            var accessSemaphore = _accessDictionary[connectionKey];
            await accessSemaphore.WaitAsync().ConfigureAwait(false);
        }

        public void Unblock(IEndpointConfiguration origin, int shardId)
        {
            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            //release associated semaphore
            var connectionKey = origin.GetConnectionKey(shardId);
            var accessSemaphore = _accessDictionary[connectionKey];
            accessSemaphore.Release();
            
        }

        private void FlushPreReceptionChecks(byte[] message, string connectionKey)
        {
            if (_flushDictionary.ContainsKey(connectionKey)) //check if flush in progress
            {
                if (message.IsFlushMessage()) //flush message returned, complete flush
                {

                    _flushDictionary.Get(connectionKey).SetResult(true);
                    _flushDictionary.Remove(connectionKey);
                    return;
                }
                else //regular message reception during flush, throw
                {
                    throw new FlushInProgressException();
                }
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
                    _accessDictionary[connectionKey] = new SemaphoreSlim(1, 1);
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
