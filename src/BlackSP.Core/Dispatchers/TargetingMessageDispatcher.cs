using BlackSP.Core.Extensions;
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
    /// <summary>
    /// Special dispatcher that can:<br/>
    /// a. target specific workers by setting partitionkey as instanceName's hashcode
    /// b. broadcast by leaving partitionkey 0.
    /// </summary>
    public class TargetingMessageDispatcher<TMessage> : IDispatcher<TMessage>
        where TMessage : IMessage
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IObjectSerializer _serializer;

        private readonly IDictionary<string, BlockingCollection<byte[]>> _outputQueues;

        private DispatchFlags _dispatchFlags;

        public TargetingMessageDispatcher(IVertexConfiguration vertexConfiguration, IObjectSerializer serializer)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _outputQueues = new Dictionary<string, BlockingCollection<byte[]>>();
            _dispatchFlags = DispatchFlags.Control;

            InitializeQueues();
        }

        public DispatchFlags GetFlags()
        {
            return _dispatchFlags;
        }

        public void SetFlags(DispatchFlags flags)
        {
            if(flags.HasFlag(DispatchFlags.Buffer))
            {
                throw new NotSupportedException($"DispatchFlags.Buffer not supported in {this.GetType()}");
            }
            _dispatchFlags = flags;
        }
        
        public BlockingCollection<byte[]> GetDispatchQueue(IEndpointConfiguration endpoint, int shardId)
        {
            _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            string endpointKey = endpoint.GetConnectionKey(shardId);
            return _outputQueues.Get(endpointKey);
        }

        public async Task Dispatch(TMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            byte[] bytes = await _serializer.SerializeAsync(message, t).ConfigureAwait(false);

            IEnumerable<string> targetConnectionKeys = !message.PartitionKey.HasValue
                ? _vertexConfiguration.OutputEndpoints.Where(e => e.IsControl == message.IsControl).SelectMany(e => e.GetAllConnectionKeys())
                : _vertexConfiguration.GetConnectionKeyByPartitionKey(message.PartitionKey.Value).Yield();

            foreach(var targetConnectionKey in targetConnectionKeys)
            {
                QueueForDispatch(targetConnectionKey, bytes, t);
            }
        }
        
        private void QueueForDispatch(string targetConnectionKey, byte[] bytes, CancellationToken t)
        {
            var shouldDispatchMessage = _dispatchFlags.HasFlag(DispatchFlags.Control);
            if (shouldDispatchMessage)
            {
                _outputQueues
                    .Get(targetConnectionKey)
                    .Add(bytes, t);
            } 
        }

        private void InitializeQueues()
        {
            foreach (var endpointConfig in _vertexConfiguration.OutputEndpoints.Where(e => e.IsControl))
            {
                var shardCount = endpointConfig.RemoteInstanceNames.Count();
                for (int shardId = 0; shardId < shardCount; shardId++)
                {
                    var endpointKey = endpointConfig.GetConnectionKey(shardId);
                    _outputQueues.Add(endpointKey, new BlockingCollection<byte[]>(Constants.DefaultThreadBoundaryQueueSize));
                }
            }
        }
    }
}
