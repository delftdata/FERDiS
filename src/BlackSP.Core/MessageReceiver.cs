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
        private BlockingCollection<IMessage> _inputBuffer;
        private ReceptionFlags _receptionFlags;
        private readonly object lockObj;

        public MessageReceiver()
        {
            _inputQueue = new BlockingCollection<IMessage>();
            _inputBuffer = new BlockingCollection<IMessage>();
            _receptionFlags = ReceptionFlags.Control & ReceptionFlags.Buffer;
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
            var typeFlag = origin.IsControl ? ReceptionFlags.Control : ReceptionFlags.Data;
            var shouldBuffer = _receptionFlags.HasFlag(ReceptionFlags.Buffer);
            if (!_receptionFlags.HasFlag(typeFlag) && shouldBuffer)
            {
                AddToInputBuffer(message);
                return;
            } 
            
            // flush blocked data input if receiving data messages and there is blocked data input
            if (!shouldBuffer && _inputBuffer.Any())
            {
                FlushBlockedDataQueue();
            }
            _inputQueue.Add(message);
        }

        public void SetFlags(ReceptionFlags mode)
        {
            _receptionFlags = mode;
        }

        /// <summary>
        /// Utility method for adding data to the blocked data queue in a thread safe manner
        /// </summary>
        /// <param name="message"></param>
        private void AddToInputBuffer(IMessage message)
        {
            lock (lockObj)
            {
                _inputBuffer.Add(message);
            }
        }

        /// <summary>
        /// Utility method to flush the blocked data queue into the inputQueue in a thread safe manner
        /// </summary>
        private void FlushBlockedDataQueue()
        {
            lock (lockObj)
            {
                _inputBuffer.CompleteAdding();
                foreach (var blockedInput in _inputBuffer.GetConsumingEnumerable())
                {
                    _inputQueue.Add(blockedInput);
                }
                _inputBuffer = new BlockingCollection<IMessage>();
            }
        }
    }
}
