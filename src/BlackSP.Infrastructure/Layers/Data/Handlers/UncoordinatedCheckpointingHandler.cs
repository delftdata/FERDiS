using BlackSP.Core.Extensions;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data.Handlers
{
    public class UncoordinatedCheckpointingHandler : IHandler<DataMessage>
    {

        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;

        private DateTime _lastCheckpointUtc;
        private TimeSpan _checkpointInterval;

        public UncoordinatedCheckpointingHandler(ICheckpointService checkpointingService, IVertexConfiguration vertexConfiguration)
        {
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _lastCheckpointUtc = DateTime.UtcNow;
            _checkpointInterval = TimeSpan.FromMinutes(1); //TODO: make configurable?
        }

        public async Task<IEnumerable<DataMessage>> Handle(DataMessage message)
        {
            if(DateTime.UtcNow - _lastCheckpointUtc > _checkpointInterval)
            {
                await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
                _lastCheckpointUtc = DateTime.UtcNow;
            }
            return message.Yield();
        }
    }
}
