using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BlackSP.Core
{
    public class MessageReceiver : IMessageReceiver
    {
        private readonly BlockingCollection<IMessage> _inputQueue;
        private BlockingCollection<IMessage> _blockedDataInputQueue;
        private ReceiverMode _mode;
        private readonly object lockObj;

        public MessageReceiver()
        {
            _inputQueue = new BlockingCollection<IMessage>();
            _blockedDataInputQueue = new BlockingCollection<IMessage>();
            _mode = ReceiverMode.Control;
            lockObj = new object();
        }

        public IEnumerable<IMessage> GetReceivedMessageEnumerator(CancellationToken t)
        {
            return _inputQueue.GetConsumingEnumerable(t);
        }

        public void Receive(IMessage message, IEndpointConfiguration origin, int shardId)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            _ = origin ?? throw new ArgumentNullException(nameof(origin));

            // block message if not receiving data and is data message
            if (!_mode.HasFlag(ReceiverMode.Data) && !origin.IsControl)
            {
                AddToBlockedDataQueue(message);
                return;
            } 
            
            // flush blocked data input if receiving data messages and there is blocked data input
            if (_mode.HasFlag(ReceiverMode.Data) && _blockedDataInputQueue.Any())
            {
                FlushBlockedDataQueue();
            }
            _inputQueue.Add(message);
        }

        public void SetMode(ReceiverMode mode)
        {
            _mode = mode;
        }

        /// <summary>
        /// Utility method for adding data to the blocked data queue in a thread safe manner
        /// </summary>
        /// <param name="message"></param>
        private void AddToBlockedDataQueue(IMessage message)
        {
            lock (lockObj)
            {
                _blockedDataInputQueue.Add(message);
            }
        }

        /// <summary>
        /// Utility method to flush the blocked data queue into the inputQueue in a thread safe manner
        /// </summary>
        private void FlushBlockedDataQueue()
        {
            lock (lockObj)
            {
                _blockedDataInputQueue.CompleteAdding();
                var blockedInputs = _blockedDataInputQueue.Take(_blockedDataInputQueue.Count);
                foreach (var blockedInput in blockedInputs)
                {
                    _inputQueue.Add(blockedInput);
                }
                _blockedDataInputQueue = new BlockingCollection<IMessage>();
            }
        }
    }
}
