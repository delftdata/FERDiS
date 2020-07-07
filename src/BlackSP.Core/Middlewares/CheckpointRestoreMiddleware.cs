using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class CheckpointRestoreMiddleware<TMessage> : IMiddleware<ControlMessage>
    {
        private readonly IVertexConfiguration _vertexConfiguration;

        public CheckpointRestoreMiddleware(IVertexConfiguration vertexConfiguration)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
        }

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!message.TryGetPayload<CheckpointRestoreRequestPayload>(out var payload))
            {
                //forward message
                return new List<ControlMessage>() { message }.AsEnumerable();
            }

            if (!payload.InstanceCheckpointMap.ContainsKey(_vertexConfiguration.InstanceName))
            {
                //checkpoint restore not targetting current vertex;
                return Enumerable.Empty<ControlMessage>();
            }

            //checkpoint restore does target current vertex
            Guid checkpointId = payload.InstanceCheckpointMap[_vertexConfiguration.InstanceName];
            Console.WriteLine($"{_vertexConfiguration.InstanceName} - Restoring checkpoint {checkpointId} (fake)");
            await Task.Delay(5000).ConfigureAwait(false); 
            //TODO: restore actual checkpoint
            Console.WriteLine($"{_vertexConfiguration.InstanceName} - Restored checkpoint {checkpointId}");

            var msg = new ControlMessage();
            msg.AddPayload(new CheckpointRestoreCompletionPayload() { 
                InstanceName = _vertexConfiguration.InstanceName, 
                CheckpointId = checkpointId 
            });
            return new List<ControlMessage>() { msg }.AsEnumerable();
        }
    }
}
