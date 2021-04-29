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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class CICPreDeliveryHandler : IHandler<DataMessage>
    {

        private readonly HMNRProtocol _hmnrProtocol;
        private readonly UncoordinatedProtocol _backupProtocol;


        private readonly ISource _source;
        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly ILogger _logger;

        public CICPreDeliveryHandler(
            HMNRProtocol hmnrProtocol,
            UncoordinatedProtocol.Factory backupProtocolFactory,
            ICheckpointService checkpointingService,
            ICheckpointConfiguration checkpointConfiguration,
            IVertexConfiguration vertexConfiguration,
            IVertexGraphConfiguration graphConfiguration,
            ILogger logger) : this(hmnrProtocol, backupProtocolFactory, null, checkpointingService, checkpointConfiguration, vertexConfiguration, graphConfiguration, logger)
        {} //source argument only optional when vertex is a source (ie does not have input channels)

        public CICPreDeliveryHandler(
            HMNRProtocol hmnrProtocol,
            UncoordinatedProtocol.Factory backupProtocolFactory,
            IReceiverSource<DataMessage> source,
            ICheckpointService checkpointingService, 
            ICheckpointConfiguration checkpointConfiguration, 
            IVertexConfiguration vertexConfiguration,
            IVertexGraphConfiguration graphConfiguration,
            ILogger logger)
        {
            _hmnrProtocol = hmnrProtocol ?? throw new ArgumentNullException(nameof(hmnrProtocol));

            _ = backupProtocolFactory ?? throw new ArgumentNullException(nameof(backupProtocolFactory));
            _ = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _backupProtocol = backupProtocolFactory.Invoke(TimeSpan.FromSeconds(checkpointConfiguration.CheckpointIntervalSeconds), default);

            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if(_vertexConfiguration.VertexType == VertexType.Operator)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
            }


            InitHMNRProtocol();
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            _checkpointingService.BeforeCheckpointTaken += () =>
            {
                _hmnrProtocol.BeforeCheckpoint();
            };
            _checkpointingService.AfterCheckpointTaken += (cpId) =>
            {
                _hmnrProtocol.AfterCheckpoint(); //NOTE: invoke after checkpoint handler to include correct clock values in checkpoint and ensure post-recovery consistency
                _backupProtocol.SetLastCheckpointUtc(DateTime.UtcNow);
            };

        }

        private void InitHMNRProtocol()
        {
            _hmnrProtocol.InitializeClocks(_vertexConfiguration.InstanceName, _graphConfiguration.InstanceNames.Where(name => !name.Contains("coordinator")).ToArray());
        }

        protected async Task HandlePayload(CICPayload payload)
        {            
            _ = payload ?? throw new ArgumentNullException(nameof(payload));
            var (org,shard) = _source.MessageOrigin;
            var originInstance = org.GetRemoteInstanceName(shard);

            if (_hmnrProtocol.CheckCheckpointCondition(originInstance, payload.clock, payload.ckpt, payload.taken))
            {
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName, true).ConfigureAwait(false);            
            }
            else if(_backupProtocol.CheckCheckpointCondition(DateTime.UtcNow))
            {
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
            }
            
            //update clocks before delivery to operator handler..
            _hmnrProtocol.BeforeDeliver(originInstance, payload.clock, payload.ckpt, payload.taken);
        }

        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if(_vertexConfiguration.VertexType == VertexType.Source)
            {
                //sources never have incoming CIC payloads.. so just check the fallback protocol instead
                if (_backupProtocol.CheckCheckpointCondition(DateTime.UtcNow))
                {
                    await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                }
            }

            if(message.TryExtractPayload<CICPayload>(out var cicPayload))
            {
                await HandlePayload(cicPayload).ConfigureAwait(false);
            }

            return message.Yield();
        }
    }
}
