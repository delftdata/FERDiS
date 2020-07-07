using BlackSP.Core.Controllers;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class DataProcessControllerMiddleware : IMiddleware<ControlMessage>
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly SingleSourceProcessController<DataMessage> _controller;
        private readonly DataProcessMonitor _processMonitor;
        private Task _activeThread;

        public DataProcessControllerMiddleware(IVertexConfiguration vertexConfiguration, SingleSourceProcessController<DataMessage> processCtrl, DataProcessMonitor dataProcessMonitor)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _controller = processCtrl ?? throw new ArgumentNullException(nameof(processCtrl));
            _processMonitor = dataProcessMonitor ?? throw new ArgumentNullException(nameof(dataProcessMonitor));
        }

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if (!message.TryGetPayload<WorkerRequestPayload>(out var payload))
            {
                return new List<ControlMessage>() { message }.AsEnumerable();
            }

            if(!payload.TargetInstanceNames.Contains(_vertexConfiguration.InstanceName))
            {
                return Enumerable.Empty<ControlMessage>();
            }

            if (payload.RequestType == WorkerRequestType.StartProcessing)
            {
                if(_activeThread == null)
                {
                    _activeThread = _controller.StartProcess();
                    _processMonitor.MarkActive(true);
                    Console.WriteLine($"{_vertexConfiguration.InstanceName} - Started data process");
                }
                else
                {
                    Console.WriteLine($"{_vertexConfiguration.InstanceName} - Data process already started");
                }

            }
            else if (payload.RequestType == WorkerRequestType.StopProcessing)
            {
                if(_activeThread != null)
                {
                    await _controller.StopProcess().ConfigureAwait(false);
                    await _activeThread.ConfigureAwait(false);
                    _activeThread = null;
                    _processMonitor.MarkActive(false);
                    Console.WriteLine($"{_vertexConfiguration.InstanceName} - Stopped data process");
                }
                else
                {
                    Console.WriteLine($"{_vertexConfiguration.InstanceName} - Data process already stopped");
                }
            }

            return Enumerable.Empty<ControlMessage>();
        }
    }
}
