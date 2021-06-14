using BlackSP.Checkpointing;
using BlackSP.Checkpointing.Persistence;
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
        private readonly IMessageLoggingService<byte[]> _loggingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ICheckpointStorage _checkpointStorage;
        private readonly Channel<ControlMessage> _output;

        public CheckpointTakenSource(ICheckpointService checkpointService, 
                                     IMessageLoggingService<byte[]> loggingService,
                                     IVertexConfiguration vertexConfiguration,
                                     ICheckpointStorage checkpointStorage) : this(checkpointService, vertexConfiguration, checkpointStorage)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            InitLoggingService();
        }

        public CheckpointTakenSource(ICheckpointService checkpointService,
                                     IVertexConfiguration vertexConfiguration,
                                     ICheckpointStorage checkpointStorage)
        {
            _checkpointService = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _checkpointStorage = checkpointStorage ?? throw new ArgumentNullException(nameof(checkpointStorage));


            _output = Channel.CreateUnbounded<ControlMessage>();

            _checkpointService.AfterCheckpointTaken += CheckpointService_AfterCheckpointTaken;
        }

        private void CheckpointService_AfterCheckpointTaken(Guid checkpointId)
        {
            var payload = new CheckpointTakenPayload
            {
                CheckpointId = checkpointId,
                OriginInstance = _vertexConfiguration.InstanceName,
                AssociatedSequenceNumbers = _loggingService != null ? new Dictionary<string, int>(_loggingService.ReceivedSequenceNumbers) : new Dictionary<string, int>(),
                MetaData = _checkpointStorage.GetMetaData(checkpointId)
            };
            if(payload.MetaData == null)
            {
                throw new InvalidOperationException($"Could not send checkpoint metadata as storage returned null for Id: {checkpointId}");
            }
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
