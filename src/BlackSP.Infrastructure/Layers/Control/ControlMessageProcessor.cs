using BlackSP.Core.MessageProcessing.Processors;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control
{
    /// <summary>
    /// Controls the message passing between underlying components<br/>
    /// Takes from any of the sources, passes to pipeline one at a time via thread synchronization and passes results to dispatcher
    /// </summary>
    public class ControlMessageProcessor : MultiSourceProcessorBase<ControlMessage>
    {
        private readonly ICheckpointService _checkpointService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;
        public ControlMessageProcessor(
            ICheckpointService checkpointService,
            IVertexConfiguration vertexConfiguration,
            IEnumerable<ISource<ControlMessage>> sources,
            IPipeline<ControlMessage> pipeline,
            IDispatcher<ControlMessage> dispatcher,
            ILogger logger) : base(sources, pipeline, dispatcher, logger) 
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task PreStartHook(CancellationToken t)
        {
            if(_vertexConfiguration.VertexType == VertexType.Coordinator)
            {
                //coordinator start means initial system startup, clear storage first
                _logger.Information("Coordinator control layer start, clearing checkpoint storage");
                await _checkpointService.ClearCheckpointStorage().ConfigureAwait(false);
                _logger.Information("Clearing checkpoint storage completed");
            }
        }
    }
}
