using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
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
    /// Special dispatcher for coordinator instances. 
    /// Can target specific Workers
    /// </summary>
    public class CoordinatorDispatcher : IDispatcher<ControlMessage>
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IMessageSerializer _serializer;

        private readonly IDictionary<string, BlockingCollection<byte[]>> _outputQueues;

        private DispatchFlags _dispatchFlags;

        public CoordinatorDispatcher(IVertexConfiguration vertexConfiguration,
                                 IMessageSerializer serializer)
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
        
        public BlockingCollection<byte[]> GetDispatchQueue(string endpointName, int shardId)
        {
            string endpointKey = ""; //TODO: key?
            return _outputQueues.Get(endpointKey);
        }

        public async Task Dispatch(ControlMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            byte[] bytes = await _serializer.SerializeMessage(message, t).ConfigureAwait(false);
            var targets = new List<string>(); //TODO: get real targets
            foreach(var targetEndpointKey in targets)
            {
                QueueForDispatch(targetEndpointKey, bytes);
            }
        }

        private void QueueForDispatch(string targetEndpointKey, byte[] bytes)
        {
            var shouldDispatchMessage = _dispatchFlags.HasFlag(DispatchFlags.Control);

            var outputQueue = _outputQueues.Get(targetEndpointKey);
            if (shouldDispatchMessage)
            {
                
                outputQueue.Add(bytes);
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
                    var endpointKey = "";//TODO: key?    /// _partitioner.GetEndpointKey(endpointName, shardCount);
                    _outputQueues.Add(endpointKey, new BlockingCollection<byte[]>());
                }
            }
        }
    }
}
