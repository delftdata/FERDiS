using BlackSP.Core.Extensions;
using BlackSP.Core.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
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

        private IEnumerable<string> AllUpstreamConnectionKeys => _vertexConfiguration.InputEndpoints.SelectMany(endpoint => endpoint.GetAllConnectionKeys());
        
        private readonly ISource<DataMessage> _messageSource;
        private readonly IReceiver<DataMessage> _messageReceiver;
        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        
        private readonly List<string> _blockedConnections;

        public CoordinatedCheckpointingHandler(ISource<DataMessage> messageSource, IReceiver<DataMessage> messageReceiver, ICheckpointService checkpointingService, IVertexConfiguration vertexConfiguration)
        {
            _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }


        protected override async Task<IEnumerable<DataMessage>> Handle(BarrierPayload payload)
        {
            var (endpoint, shardId) = _messageSource.MessageOrigin;
            var connectionKey = endpoint.GetConnectionKey(shardId);
            if(_blockedConnections.Contains(connectionKey))
            {
                throw new InvalidOperationException("Received two barriers from one connection");
            }
            _messageReceiver.Block(endpoint, shardId);
            _blockedConnections.Add(connectionKey);
            //if all upstream connections are blocked..
            if(_blockedConnections.Intersect(AllUpstreamConnectionKeys).Count() == AllUpstreamConnectionKeys.Count())
            {
                //take a checkpoint
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                foreach(var _ in AllUpstreamConnectionKeys)
                {   //and unblock all connections
                    _messageReceiver.Unblock(endpoint, shardId);
                }
                //re-add the barrier to propagate it downstream
                AssociatedMessage.AddPayload(payload); 
            }
            //TODO: ensure broadcast
            return AssociatedMessage.Yield();
        }
    }
}
