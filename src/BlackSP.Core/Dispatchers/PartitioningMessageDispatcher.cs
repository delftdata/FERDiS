using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private readonly IDictionary<string, IFlushableQueue<byte[]>> _outputQueues;

        public PartitioningMessageDispatcher(IVertexConfiguration vertexConfiguration,
                                 IObjectSerializer serializer,
                                 IPartitioner<IMessage> partitioner)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(serializer));

            _outputQueues = new Dictionary<string, IFlushableQueue<byte[]>>();

            InitializeQueues();
        }
        
        public IFlushableQueue<byte[]> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId)
        {
            _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            string endpointKey =  endpoint.GetConnectionKey(shardId);
            return _outputQueues.Get(endpointKey);
        }

        public async Task Dispatch(TMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            byte[] bytes = await _serializer.SerializeAsync(message, t).ConfigureAwait(false);

            foreach(var targetConnectionKey in _partitioner.Partition(message))
            {
                QueueForDispatch(targetConnectionKey, bytes, t);
            }
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        private void QueueForDispatch(string targetConnectionKey, byte[] bytes, CancellationToken t)
        {
            var outputQueue = _outputQueues.Get(targetConnectionKey);
            outputQueue.Add(bytes, t); 
        }

        private void InitializeQueues()
        {
            foreach (var endpointConfig in _vertexConfiguration.OutputEndpoints)
            {
                var shardCount = endpointConfig.RemoteInstanceNames.Count();
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var connectionKey = endpointConfig.GetConnectionKey(shardId);
                    _outputQueues.Add(connectionKey, new BlockingFlushableQueue<byte[]>(Constants.DefaultThreadBoundaryQueueSize));
                }
            }
        }

        public async Task BeginFlush()
        {
            await Task.WhenAll(_outputQueues.Values.Select(q => q.BeginFlush())).ConfigureAwait(false);
        }

        public async Task EndFlush()
        {
            await Task.WhenAll(_outputQueues.Values.Where(q => q.IsFlushing).Select(q => q.EndFlush())).ConfigureAwait(false);
        }
    }
}
