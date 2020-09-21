using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Processors
{
    /// <summary>
    /// Controls the message passing between underlying components<br/>
    /// Takes from any of the sources, passes to pipeline one at a time via thread synchronization and passes results to dispatcher
    /// </summary>
    public class ControlMessageProcessor : MultiSourceProcessorBase<ControlMessage>
    {
        private readonly ICheckpointService _checkpointService;
        private readonly IVertexConfiguration _vertexConfiguration;
        public ControlMessageProcessor(
            ICheckpointService checkpointService,
            IVertexConfiguration vertexConfiguration,
            IEnumerable<ISource<ControlMessage>> sources,
            IPipeline<ControlMessage> pipeline,
            IDispatcher<ControlMessage> dispatcher) : base(sources, pipeline, dispatcher) 
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }

        public override async Task StartProcess(CancellationToken t)
        {
            if(_vertexConfiguration.VertexType != VertexType.Coordinator)
            {
                await _checkpointService.TakeInitialCheckpointIfNotExists(_vertexConfiguration.InstanceName).ConfigureAwait(false);
            }
            await base.StartProcess(t).ConfigureAwait(false);
        }
    }
}
