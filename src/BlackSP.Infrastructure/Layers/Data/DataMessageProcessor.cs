using BlackSP.Core.Models;
using BlackSP.Core.Processors;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data
{
    /// <summary>
    /// Controls the message passing between underlying components<br/>
    /// Takes data messages from the source, passes to pipeline one at a time and passes results to dispatcher
    /// </summary>
    public class DataMessageProcessor : SingleSourceProcessorBase<DataMessage>
    {
        private readonly ICheckpointService _checkpointService;
        private readonly IVertexConfiguration _vertexConfiguration;

        private readonly ISource<DataMessage> _source;
        private readonly IDispatcher<DataMessage> _dispatcher;
        private readonly ILogger _logger;

        public DataMessageProcessor(ICheckpointService checkpointService,
            IVertexConfiguration vertexConfiguration,
            ISource<DataMessage> source,
            IPipeline<DataMessage> pipeline,
            IDispatcher<DataMessage> dispatcher,
            ILogger logger) : base(source, pipeline, dispatcher, logger)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task StartProcess(CancellationToken t)
        {
            var instanceName = _vertexConfiguration.InstanceName;
            if (_checkpointService.GetLastCheckpointId(instanceName) == default)
            {
                //no checkpoint has been taken nor restored yet, checkpoint initial state first
                _logger.Information("No known last checkpointId, taking initial checkpoint");
                var sw = new Stopwatch();
                sw.Start();
                await _checkpointService.TakeCheckpoint(instanceName).ConfigureAwait(false);
                sw.Stop();
                _logger.Information($"Initial state succesfully checkpointed in {sw.ElapsedMilliseconds}ms");
            } 
            else
            {
                _logger.Information($"No initial checkpoint required, proceeding with data layer start");
            }
            await base.StartProcess(t).ConfigureAwait(false);
        }

        public async Task Flush(IEnumerable<string> upstreamInstancesToFlush, IEnumerable<string> downstreamInstancesToFlush)
        {
            var upstreamFlush = _source.Flush(upstreamInstancesToFlush);
            var downstreamFlush = _dispatcher.Flush(downstreamInstancesToFlush);//TODO: do not flush a failed downstream instance!
            await Task.WhenAll(upstreamFlush, downstreamFlush).ConfigureAwait(false);
            _logger.Verbose("DataMessageProcessor flushed input and output successfully");
        }
    }
}
