using BlackSP.Checkpointing.Protocols;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data.Payloads;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class CommunicationInducedCheckpointingHandler : ForwardingPayloadHandlerBase<DataMessage, CICPayload>
    {

        private readonly HMNRProtocol _hmnrProtocol;
        private readonly UncoordinatedProtocol _backupProtocol;


        private readonly ISource _source;
        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        public CommunicationInducedCheckpointingHandler(
            HMNRProtocol hmnrProtocol,
            UncoordinatedProtocol.Factory backupProtocolFactory,
            IReceiverSource<DataMessage> source,
            ICheckpointService checkpointingService, 
            ICheckpointConfiguration checkpointConfiguration, 
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _hmnrProtocol = hmnrProtocol ?? throw new ArgumentNullException(nameof(hmnrProtocol));

            _ = backupProtocolFactory ?? throw new ArgumentNullException(nameof(backupProtocolFactory));
            _ = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _backupProtocol = backupProtocolFactory.Invoke(TimeSpan.FromSeconds(checkpointConfiguration.CheckpointIntervalSeconds), default);

            _source = source ?? throw new ArgumentNullException(nameof(source));
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        protected override async Task<IEnumerable<DataMessage>> Handle(CICPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            var (org,shard) = _source.MessageOrigin;
            var originInstance = org.GetRemoteInstanceName(shard);


            if (_hmnrProtocol.CheckCheckpointCondition(originInstance, payload.clock, payload.ckpt, payload.taken))
            {
                _hmnrProtocol.BeforeCheckpoint();
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                _hmnrProtocol.AfterCheckpoint();
            } 
            else if(_backupProtocol.CheckCheckpointCondition(DateTime.UtcNow))
            {
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                _backupProtocol.SetLastCheckpointUtc(DateTime.UtcNow);
            }
            
            //update clocks before delivery to operator handler..
            _hmnrProtocol.BeforeDeliver(originInstance, payload.clock, payload.ckpt, payload.taken);

            return AssociatedMessage.Yield();
        }
    }
}
