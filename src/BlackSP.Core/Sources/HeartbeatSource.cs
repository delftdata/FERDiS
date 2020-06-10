using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Sources
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
            throw new NotImplementedException();
        }

        public ControlMessage Take(CancellationToken t)
        {
            var spanSinceLastBeat = DateTime.Now - _lastHeartBeat;
            if (spanSinceLastBeat.TotalSeconds >= _hbFrequencySeconds)
            {
                return new ControlMessage(null);
            }
            return null;
        }
    }
}
