using BlackSP.Core.Extensions;
using BlackSP.Core.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    /// <summary>
    /// Reception handler for checkpoint dependency information payloads
    /// </summary>
    public class CheckpointDependencyTrackingReceptionHandler : ForwardingPayloadHandlerBase<DataMessage, CheckpointDependencyPayload>
    {

        private readonly ICheckpointService _checkpointService;
        private readonly ISource<DataMessage> _messageSource;
        
        public CheckpointDependencyTrackingReceptionHandler(ICheckpointService checkpointService, 
            ISource<DataMessage> messageSource)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
        }

        protected override Task<IEnumerable<DataMessage>> Handle(CheckpointDependencyPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            var (endpoint, shardId) = _messageSource.MessageOrigin;
            _ = endpoint ?? throw new InvalidOperationException("Message source returned null endpoint origin");
            _checkpointService.UpdateCheckpointDependency(endpoint.GetRemoteInstanceName(shardId), payload.CheckpointId);
            return Task.FromResult(AssociatedMessage.Yield());
        }
    }
}
