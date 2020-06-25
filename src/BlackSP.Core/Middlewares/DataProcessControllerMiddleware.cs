using BlackSP.Core.Controllers;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class DataProcessControllerMiddleware : IMiddleware<ControlMessage>
    {

        private readonly SingleSourceProcessController<DataMessage> _controller;

        private Task _activeThread;

        public DataProcessControllerMiddleware(SingleSourceProcessController<DataMessage> processCtrl)
        {
            _controller = processCtrl ?? throw new ArgumentNullException(nameof(processCtrl));
        }

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if (!message.TryGetPayload<WorkerRequestPayload>(out var payload))
            {
                return new List<ControlMessage>() { message }.AsEnumerable();
            }

            if(payload.RequestType == WorkerRequestType.StartProcessing)
            {
                if(_activeThread == null)
                {
                    _activeThread = _controller.StartProcess();
                    Console.WriteLine("Data process started");
                } else
                {
                    Console.WriteLine("Data process already started");
                }

            }
            else if (payload.RequestType == WorkerRequestType.StopProcessing)
            {
                await _controller.StopProcess().ConfigureAwait(false);
                if(_activeThread != null)
                {
                    await _activeThread.ConfigureAwait(false);
                    _activeThread = null;
                }
            }

            return Enumerable.Empty<ControlMessage>();
        }
    }
}
