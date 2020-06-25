using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.MessageSources
{
    public class HeartbeatSource : IMessageSource<ControlMessage>
    {
        private readonly int _hbFrequencySeconds;
        private DateTime _lastHeartBeat;

        public HeartbeatSource()
        {
            _lastHeartBeat = DateTime.MinValue;
            _hbFrequencySeconds = 3;

        }

        public Task Flush()
        {
            return Task.CompletedTask; //nothing to flush here
        }

        public ControlMessage Take(CancellationToken t)
        {

            //TODO: rewrite to timer that fills blockingcollection?
            while ((DateTime.Now - _lastHeartBeat).TotalSeconds < _hbFrequencySeconds)
            {
                Thread.Sleep(100);
            }
            _lastHeartBeat = DateTime.Now;
            var msg = new ControlMessage();
            msg.AddPayload(new WorkerRequestPayload { RequestType = WorkerRequestType.StartProcessing });
            Console.WriteLine("Generated heartbeat");
            return msg;
        }
    }
}
