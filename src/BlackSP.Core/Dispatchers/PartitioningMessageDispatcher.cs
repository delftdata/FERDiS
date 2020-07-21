﻿using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
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
    public class MessageDispatcher : IDispatcher<IMessage>, IDispatcher<DataMessage>, IDispatcher<ControlMessage>
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IObjectSerializer<IMessage> _serializer;
        private readonly IPartitioner<IMessage> _partitioner;

        private readonly IDictionary<string, BlockingCollection<byte[]>> _outputQueues;
        private readonly IDictionary<string, ICollection<byte[]>> _outputBuffers;

        private DispatchFlags _dispatchFlags;

        public MessageDispatcher(IVertexConfiguration vertexConfiguration,
                                 IObjectSerializer<IMessage> serializer,
                                 IPartitioner<IMessage> partitioner)
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
        
        public BlockingCollection<byte[]> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId)
        {
            _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            string endpointKey =  endpoint.GetConnectionKey(shardId);
            return _outputQueues.Get(endpointKey);
        }

        public async Task Dispatch(IMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            byte[] bytes = await _serializer.SerializeAsync(message, t).ConfigureAwait(false);


            foreach(var targetConnectionKey in _partitioner.Partition(message))
            {
                QueueForDispatch(targetConnectionKey, bytes, message.IsControl, t);
            }
        }

        public async Task Dispatch(DataMessage message, CancellationToken t) 
            => await Dispatch((IMessage)message, t).ConfigureAwait(false);

        public async Task Dispatch(ControlMessage message, CancellationToken t)
            => await Dispatch((IMessage)message, t).ConfigureAwait(false);

        private void QueueForDispatch(string targetConnectionKey, byte[] bytes, bool isControl, CancellationToken t)
        {
            var shouldDispatchMessage = _dispatchFlags.HasFlag(isControl ? DispatchFlags.Control : DispatchFlags.Data);
            var shouldBuffer = _dispatchFlags.HasFlag(DispatchFlags.Buffer);

            var outputQueue = _outputQueues.Get(targetConnectionKey);
            var outputBuffer = _outputBuffers.Get(targetConnectionKey);
            if (shouldDispatchMessage)
            {
                lock(outputBuffer) //TODO: consider removing lock statements
                {
                    FlushBuffer(outputBuffer, outputQueue);
                }
                outputQueue.Add(bytes, t);
            } 
            else if(shouldBuffer)
            {
                lock (outputBuffer)
                {
                    outputBuffer.Add(bytes);
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
                var shardCount = endpointConfig.RemoteInstanceNames.Count();
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var connectionKey = endpointConfig.GetConnectionKey(shardId);
                    _outputQueues.Add(connectionKey, new BlockingCollection<byte[]>(1 << 14)); //CAPACITY??
                    _outputBuffers.Add(connectionKey, new List<byte[]>());
                }
            }
        }
    }
}