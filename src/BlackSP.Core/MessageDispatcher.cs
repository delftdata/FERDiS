using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IMessageSerializer _serializer;
        private readonly IMessagePartitioner _partitioner;

        private readonly IDictionary<string, BlockingCollection<byte[]>> _outputQueues;
        
        public MessageDispatcher(IVertexConfiguration vertexConfiguration,
                                 IMessageSerializer serializer,
                                 IMessagePartitioner partitioner)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(serializer));

            _outputQueues = new Dictionary<string, BlockingCollection<byte[]>>();
            InitializeQueues();
        }

        public async Task Dispatch(IEnumerable<IMessage> messages, CancellationToken t)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            foreach(var message in messages)
            {
                byte[] bytes = await _serializer.SerializeMessage(message, t).ConfigureAwait(false);
                foreach(var targetEndpointKey in _partitioner.Partition(message))
                {
                    GetDispatchQueue(targetEndpointKey).Add(bytes);
                }
            }
        }

        public BlockingCollection<byte[]> GetDispatchQueue(string endpointName, int shardId)
        {
            string endpointKey = _partitioner.GetEndpointKey(endpointName, shardId);
            return GetDispatchQueue(endpointKey);
        }

        private BlockingCollection<byte[]> GetDispatchQueue(string endpointKey)
        {
            BlockingCollection<byte[]> result;
            if (_outputQueues.TryGetValue(endpointKey, out result))
            {
                return result;
            }
            throw new ArgumentOutOfRangeException($"No control or data shard-queue found for endpoint: {endpointKey}");

        }

        private void InitializeQueues()
        {
            foreach (var endpointConfig in _vertexConfiguration.OutputEndpoints)
            {
                var endpointName = endpointConfig.RemoteEndpointName;
                var shardCount = endpointConfig.RemoteShardCount;
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    _outputQueues.Add(_partitioner.GetEndpointKey(endpointName, shardCount), new BlockingCollection<byte[]>());
                }
            }
        }
    }
}
