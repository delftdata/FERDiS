using BlackSP.Core.Extensions;
using BlackSP.Core.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    /// <summary>
    /// Implements chandy-lamports barrier algorithm for checkpoint decision making
    /// </summary>
    public class CoordinatedCheckpointingHandler : ForwardingPayloadHandlerBase<DataMessage, BarrierPayload>
    {

        private IEnumerable<string> AllUpstreamConnectionKeys => _vertexConfiguration.InputEndpoints.Where(endpoint => !endpoint.IsControl).SelectMany(endpoint => endpoint.GetAllConnectionKeys());
        
        private readonly ISource<DataMessage> _messageSource;
        private readonly IReceiver<DataMessage> _messageReceiver;
        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        private readonly List<(IEndpointConfiguration, int)> _blockedConnections;
        private readonly List<string> _blockedConnectionKeys;
        public CoordinatedCheckpointingHandler(ISource<DataMessage> messageSource, 
            IReceiver<DataMessage> messageReceiver, 
            ICheckpointService checkpointingService, 
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
            _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _blockedConnections = new List<(IEndpointConfiguration, int)>();
            _blockedConnectionKeys = new List<string>();
        }

        public CoordinatedCheckpointingHandler(ISource<DataMessage> messageSource,
            ICheckpointService checkpointingService,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
            _messageReceiver = null;
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _blockedConnections = new List<(IEndpointConfiguration, int)>();
            _blockedConnectionKeys = new List<string>();
        }


        protected override async Task<IEnumerable<DataMessage>> Handle(BarrierPayload payload)
        {
            var (endpoint, shardId) = _messageSource.MessageOrigin;
            if(_vertexConfiguration.VertexType == VertexType.Source)
            {
                await TakeCheckpoint().ConfigureAwait(false);
                AssociatedMessage.AddPayload(payload); //re-add to ensure propagation
            } 
            else
            {
                await PerformBarrierBlocking(payload, endpoint, shardId).ConfigureAwait(false);
            }
            
            if(AssociatedMessage.PartitionKey.HasValue)
            {   //fail-safe to ensure correct inner working of the implementation
                throw new InvalidOperationException("Barrier message should never have a PartitionKey");
            }
            return AssociatedMessage.Yield();
        }

        /// <summary>
        /// Utility method that performs barrier blocking - checkpointing and barrier unblocking
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="endpoint"></param>
        /// <param name="shardId"></param>
        /// <returns></returns>
        private async Task PerformBarrierBlocking(BarrierPayload payload, IEndpointConfiguration endpoint, int shardId)
        {
            var connectionKey = endpoint.GetConnectionKey(shardId);
            if (_blockedConnectionKeys.Contains(connectionKey))
            {
                _logger.Debug($"Duplicate connectionKey encountered: {connectionKey}");
                throw new InvalidOperationException($"Received two barriers from one connection - {endpoint.RemoteVertexName} at {endpoint.GetRemoteInstanceName(shardId)} shard {shardId}");
            }
            _logger.Information($"Received barrier, proceeding to block connection to instance {endpoint.GetRemoteInstanceName(shardId)}");
            _messageReceiver.Block(endpoint, shardId);
            _blockedConnections.Add((endpoint, shardId));
            _blockedConnectionKeys.Add(connectionKey);
            //if all upstream connections are blocked..
            if (_blockedConnectionKeys.Intersect(AllUpstreamConnectionKeys).Count() == AllUpstreamConnectionKeys.Count())
            {
                //take a checkpoint
                await TakeCheckpoint().ConfigureAwait(false);
                //and unblock all connections
                UnblockAllConnections();
                //re-add the barrier to propagate it downstream
                AssociatedMessage.AddPayload(payload);
                _logger.Information($"Unblocked {AllUpstreamConnectionKeys.Count()} upstream connections and forwarded barrier");
            }
        }

        private void UnblockAllConnections()
        {
            foreach (var (ep, sId) in _blockedConnections)
            {   
                _messageReceiver.Unblock(ep, sId);
            }
            _blockedConnections.Clear();
            _blockedConnectionKeys.Clear();
        }

        /// <summary>
        /// Utility method that just performs checkpointing
        /// </summary>
        /// <returns></returns>
        private async Task TakeCheckpoint()
        {
            var stopwatch = new Stopwatch();
            _logger.Information("Received barrier from every upstream, proceeding to take checkpoint");
            stopwatch.Start();
            var newCpId = await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.Information($"Checkpoint {newCpId} successfully taken in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
