using BlackSP.Core.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IDictionary<string, ICollection<byte[]>> _outputBuffers;

        private DispatchFlags _dispatchFlags;

        public MessageDispatcher(IVertexConfiguration vertexConfiguration,
                                 IMessageSerializer serializer,
                                 IMessagePartitioner partitioner)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(serializer));

            _outputQueues = new Dictionary<string, BlockingCollection<byte[]>>();
            _outputBuffers = new Dictionary<string, ICollection<byte[]>>();
            _dispatchFlags = DispatchFlags.Control & DispatchFlags.Buffer;

            InitializeQueues();
        }

        public DispatchFlags GetFlags()
        {
            return _dispatchFlags;
        }

        public void SetFlags(DispatchFlags flags)
        {
            _dispatchFlags = flags;
        }
        
        public BlockingCollection<byte[]> GetDispatchQueue(string endpointName, int shardId)
        {
            string endpointKey = _partitioner.GetEndpointKey(endpointName, shardId);
            return _outputQueues.Get(endpointKey);
        }

        public async Task Dispatch(IEnumerable<IMessage> messages, CancellationToken t)
        {
            _ = messages ?? throw new ArgumentNullException(nameof(messages));
            foreach(var message in messages)
            {
                byte[] bytes = await _serializer.SerializeMessage(message, t).ConfigureAwait(false);
                foreach(var targetEndpointKey in _partitioner.Partition(message))
                {
                    Dispatch(targetEndpointKey, bytes, message.IsControl);
                }
            }
        }

        private void Dispatch(string targetEndpointKey, byte[] bytes, bool isControl)
        {
            var shouldDispatchMessage = _dispatchFlags.HasFlag(isControl ? DispatchFlags.Control : DispatchFlags.Data);
            var shouldBuffer = _dispatchFlags.HasFlag(DispatchFlags.Buffer);

            var outputQueue = _outputQueues.Get(targetEndpointKey);
            var outputBuffer = _outputBuffers.Get(targetEndpointKey);
            if (shouldDispatchMessage)
            {
                lock(outputBuffer)
                {
                    FlushBuffer(outputBuffer, outputQueue);
                }
                outputQueue.Add(bytes);
            } 
            else if(shouldBuffer)
            {
                lock (outputBuffer)
                {
                    _outputBuffers.Get(targetEndpointKey).Add(bytes);
                }
            }
        }

        /// <summary>
        /// Utility method that flushes an output buffer
        /// </summary>
        /// <param name="outputBuffer"></param>
        /// <param name="outputQueue"></param>
        /// <param name="targetEndpointKey"></param>
        private void FlushBuffer(ICollection<byte[]> outputBuffer, BlockingCollection<byte[]> outputQueue)
        {
            if (outputBuffer.Any())
            {
                foreach (var msg in outputBuffer)
                {
                    outputQueue.Add(msg);
                }
                outputBuffer.Clear();
            }
        }

        private void InitializeQueues()
        {
            foreach (var endpointConfig in _vertexConfiguration.OutputEndpoints)
            {
                var endpointName = endpointConfig.RemoteEndpointName;
                var shardCount = endpointConfig.RemoteShardCount;
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var endpointKey = _partitioner.GetEndpointKey(endpointName, shardCount);
                    _outputQueues.Add(endpointKey, new BlockingCollection<byte[]>());
                    _outputBuffers.Add(endpointKey, new List<byte[]>());
                }
            }
        }
    }
}
