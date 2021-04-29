using BlackSP.Checkpointing;
using BlackSP.Checkpointing.Protocols;
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

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class UncoordinatedCheckpointingHandler : IHandler<DataMessage>
    {

        private readonly UncoordinatedProtocol _protocol;
        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ICheckpointConfiguration _checkpointConfiguration;
        private readonly ILogger _logger;

        public UncoordinatedCheckpointingHandler(UncoordinatedProtocol.Factory protocolFactory,
            ICheckpointService checkpointingService,
            ICheckpointConfiguration checkpointConfiguration,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _checkpointConfiguration = checkpointConfiguration ?? throw new ArgumentNullException(nameof(checkpointConfiguration));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _protocol = protocolFactory?.Invoke(TimeSpan.FromSeconds(_checkpointConfiguration.CheckpointIntervalSeconds), default);
        }

        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message, CancellationToken t)
        {

            if (_protocol.CheckCheckpointCondition(DateTime.UtcNow))
            {
                _logger.Information($"Uncoordinated checkpoint will be taken, configured interval is {_checkpointConfiguration.CheckpointIntervalSeconds}s");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var cpId = await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                sw.Stop();
                _logger.Information($"Checkpoint {cpId} has been taken in {sw.ElapsedMilliseconds}ms");
                _protocol.SetLastCheckpointUtc(DateTime.UtcNow);
            }
            return message.Yield();
        }
    }
}
