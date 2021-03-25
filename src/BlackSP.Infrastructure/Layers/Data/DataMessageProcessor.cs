using BlackSP.Checkpointing;
using BlackSP.Core.MessageProcessing.Processors;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly ICheckpointConfiguration _checkpointConfiguration;
        private readonly ConnectionMonitor _connectionMonitor;

        private readonly ISource<DataMessage> _source;
        private readonly IDispatcher<DataMessage> _dispatcher;
        private readonly ILogger _logger;

        public DataMessageProcessor(ICheckpointService checkpointService,
            IVertexConfiguration vertexConfiguration,
            ICheckpointConfiguration checkpointConfiguration,
            ConnectionMonitor connectionMonitor,
            ISource<DataMessage> source,
            IPipeline<DataMessage> pipeline,
            IDispatcher<DataMessage> dispatcher,
            ILogger logger) : base(source, pipeline, dispatcher, logger)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task PreStartHook(CancellationToken t)
        {
            var instanceName = _vertexConfiguration.InstanceName;
            if (_checkpointService.GetLastCheckpointId(instanceName) == default)
            {
                //no checkpoint has been taken nor restored yet, checkpoint initial state first
                _logger.Information("No known last checkpointId, taking initial checkpoint");
                var sw = new Stopwatch();
                sw.Start();
                var cpId = await _checkpointService.TakeCheckpoint(instanceName).ConfigureAwait(false);
                sw.Stop();
                _logger.Information($"Initial checkpoint {cpId} succesfully taken in {sw.ElapsedMilliseconds}ms");
            } 
            else
            {
                _logger.Information($"No initial checkpoint required, proceeding with data layer start");
            }

            if (!_checkpointConfiguration.AllowReusingState) {
                //when reusing state is disallowed we are sure to receive a checkpoint restore request when a downstream instance fails
                //when that happens: stop processing and flush the dispatchqueue to the failed instance
                _connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionFail_StopProcessAndFlushDispatcher;
            }
        }

        private void ConnectionMonitor_OnConnectionFail_StopProcessAndFlushDispatcher(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            var (connection, isactive) = e.ChangedConnection;
            if(!connection.IsUpstream && !isactive)
            {
                var failedInstanceName = connection.Endpoint.GetRemoteInstanceName(connection.ShardId);
                _logger.Information($"Failure detected in downstream instance {failedInstanceName}");
                try
                {
                    StopProcess().Wait();
                }
                catch {}

                var dispatchQueueToFailedInstance = _dispatcher.GetDispatchQueue(connection.Endpoint, connection.ShardId);
                Task.WhenAll(dispatchQueueToFailedInstance.BeginFlush(), dispatchQueueToFailedInstance.EndFlush()).Wait();
                _logger.Information($"Processor halted due to downstream failure in {failedInstanceName}");
            }
        }

        public async Task Flush(IEnumerable<string> upstreamInstancesToFlush, IEnumerable<string> downstreamInstancesToFlush)
        {
            _logger.Verbose("DataMessageProcessor starting to flush source and dispatcher");

            var upstreamFlush = _source.Flush(upstreamInstancesToFlush);
            var downstreamFlush = _dispatcher.Flush(downstreamInstancesToFlush);
            await Task.WhenAll(upstreamFlush, downstreamFlush).ConfigureAwait(false);
            _logger.Debug("DataMessageProcessor flushed input and output successfully");
        }
    }
}
