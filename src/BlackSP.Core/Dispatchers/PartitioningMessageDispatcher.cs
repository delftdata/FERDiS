﻿using BlackSP.Core.Extensions;
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

namespace BlackSP.Core.Dispatchers
{
    /// <summary>
    /// Dispatcher capable of dispatching any implementation of IMessage. Does so by utilizing a provided IPartitioner.
    /// </summary>
    public class PartitioningMessageDispatcher<TMessage> : IDispatcher<TMessage>
        where TMessage : IMessage
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IObjectSerializer _serializer;
        private readonly IPartitioner<IMessage> _partitioner;
        private readonly ILogger _logger;

        private readonly IDictionary<string, FlushableChannel<byte[]>> _outputQueues;
        private readonly IDictionary<string, (IEndpointConfiguration, int)> _originDict;
        public PartitioningMessageDispatcher(IVertexConfiguration vertexConfiguration,
                                 IObjectSerializer serializer,
                                 IPartitioner<IMessage> partitioner,
                                 ILogger logger)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _outputQueues = new Dictionary<string, FlushableChannel<byte[]>>();
            _originDict = new Dictionary<string, (IEndpointConfiguration, int)>();
            InitializeQueues();
        }
       

        public IFlushable<Channel<byte[]>> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId)
        {
            _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            string endpointKey = endpoint.GetConnectionKey(shardId);
            return _outputQueues.Get(endpointKey);
        }

        public async Task Dispatch(TMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            byte[] bytes = await _serializer.SerializeAsync(message, t).ConfigureAwait(false);

            foreach(var targetConnectionKey in _partitioner.Partition(message))
            {
                var outputQueue = _outputQueues.Get(targetConnectionKey);
                await outputQueue.UnderlyingCollection.Writer.WriteAsync(bytes, t);
            }
        }

        private void InitializeQueues()
        {
            foreach (var endpointConfig in _vertexConfiguration.OutputEndpoints)
            {
                
                var shardCount = endpointConfig.RemoteInstanceNames.Count();
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var connectionKey = endpointConfig.GetConnectionKey(shardId);
                    _outputQueues.Add(connectionKey, new FlushableChannel<byte[]>(Constants.DefaultThreadBoundaryQueueSize));
                    _originDict.Add(connectionKey, (endpointConfig, shardId));
                }
            }
        }

        public async Task Flush(IEnumerable<string> downstreamInstancesToFlush)
        {
            var flushes = GetQueuesByInstanceNames(downstreamInstancesToFlush).Select(q => q.BeginFlush()).ToList();
            _logger.Debug($"Dispatcher flushing {flushes.Count}/{_outputQueues.Count} queues");
            await Task.WhenAll(flushes).ConfigureAwait(false);
            _logger.Debug($"Dispatcher flushed {flushes.Count}/{_outputQueues.Count} queues");

        }

        private IEnumerable<FlushableChannel<byte[]>> GetQueuesByInstanceNames(IEnumerable<string> instanceNames)
        {
            return _outputQueues.Where(p =>
            {
                var (endpoint, shardId) = _originDict[p.Key];
                return instanceNames.Contains(endpoint.GetRemoteInstanceName(shardId));
            }).Select(p => p.Value);
        }

        
    }
}
