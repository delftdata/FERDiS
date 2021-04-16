using BlackSP.Checkpointing;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Control.Sources
{
    /// <summary>
    /// Generates a message when the process takes a checkpoint.
    /// </summary>
    public class CheckpointTakenSource : ISource<ControlMessage>
    {
        public (IEndpointConfiguration, int) MessageOrigin => (default, default);

        private readonly ICheckpointService _checkpointService;
        private readonly IMessageLoggingService<DataMessage> _loggingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly Channel<ControlMessage> _output;

        public CheckpointTakenSource(ICheckpointService checkpointService, 
                                     IMessageLoggingService<DataMessage> loggingService,
                                     IVertexConfiguration vertexConfiguration) : this(checkpointService, vertexConfiguration)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            InitLoggingService();
        }

        public CheckpointTakenSource(ICheckpointService checkpointService,
                                     IVertexConfiguration vertexConfiguration)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));


            _output = Channel.CreateUnbounded<ControlMessage>();

            _checkpointService.AfterCheckpointTaken += CheckpointService_AfterCheckpointTaken;
        }

        private void CheckpointService_AfterCheckpointTaken(Guid checkpointId)
        {
            var payload = new CheckpointTakenPayload
            {
                CheckpointId = checkpointId,
                OriginInstance = _vertexConfiguration.InstanceName,
                AssociatedSequenceNumbers = _loggingService?.ReceivedSequenceNumbers
            };
            var msg = new ControlMessage();
            msg.AddPayload(payload);
            while(!_output.Writer.TryWrite(msg))
            { /*try again..*/ }
        }

        public Task Flush(IEnumerable<string> upstreamInstancesToFlush)
        {
            return Task.CompletedTask;
        }

        public async Task<ControlMessage> Take(CancellationToken t)
        {
            return await _output.Reader.ReadAsync(t);
        }

        private void InitLoggingService()
        {
            var upstreams = _vertexConfiguration.InputEndpoints.Where(e => !e.IsControl).SelectMany(e => e.RemoteInstanceNames).ToArray();
            var downstreams = _vertexConfiguration.OutputEndpoints.Where(e => !e.IsControl).SelectMany(e => e.RemoteInstanceNames).ToArray();

            _loggingService.Initialize(downstreams, upstreams);
        }

        
    }
}
