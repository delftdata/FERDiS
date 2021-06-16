using BlackSP.Checkpointing.Persistence;
using BlackSP.Core.Coordination;
using BlackSP.Core.Extensions;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Infrastructure.Layers.Control.Sources;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler that handles responses of worker requests on the coordinator side
    /// </summary>
    public class GlobalCheckpointTakenHandler : ForwardingPayloadHandlerBase<ControlMessage, CheckpointTakenPayload>
    {


        private readonly ChandyLamportBarrierSource _barrierSource;
        private readonly ICheckpointStorage _checkpointStorage;
        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        private List<string> checkpointedInstances;
        private List<string> allInstanceNames;


        public GlobalCheckpointTakenHandler(ChandyLamportBarrierSource barrierSource,
            ICheckpointStorage checkpointStorage,
            IVertexGraphConfiguration graphConfiguration,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _barrierSource = barrierSource ?? throw new ArgumentNullException(nameof(barrierSource));
            _checkpointStorage = checkpointStorage ?? throw new ArgumentNullException(nameof(checkpointStorage));
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            checkpointedInstances = new List<string>();
            allInstanceNames = _graphConfiguration.InstanceNames.Where(n => !n.Contains("coordinator")).ToList();
        }

        protected override Task<IEnumerable<ControlMessage>> Handle(CheckpointTakenPayload payload, CancellationToken t)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            _logger.Debug("Handling CheckpointTakenPayload from " + payload.OriginInstance + " with checkpoint " + payload.CheckpointId);

            if (payload.MetaData == null)
            {
                _logger.Warning("Received checkpoint taken payload without metadata");
            }

            payload.MetaData.Dependencies ??= new Dictionary<string, Guid>();
            _checkpointStorage.AddMetaData(payload.MetaData);

            checkpointedInstances.Add(payload.OriginInstance);
            if(checkpointedInstances.Intersect(allInstanceNames).Count() == allInstanceNames.Count)
            {
                _logger.Information("Global checkpoint taken");
                checkpointedInstances.Clear();
                _barrierSource.CheckpointTimer(true);
            }
            return Task.FromResult(AssociatedMessage.Yield());
        }

    }
}
