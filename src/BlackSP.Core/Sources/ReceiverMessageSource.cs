using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Sources
{
    /// <summary>
    /// Receives input from any source. Exposes received messages through the IMessageSource interface.<br/>
    /// Sorts and orders input based on message types to be consumed one-by-one.
    /// </summary>
    public sealed class ReceiverMessageSource : IReceiver, ISource<DataMessage>, ISource<ControlMessage>
    {
        private BlockingCollection<DataMessage> _dataQueue;
        private BlockingCollection<ControlMessage> _controlQueue;
        private BlockingCollection<IMessage> _inputBuffer;
        private ReceptionFlags _receptionFlags;
        private readonly object lockObj;

        public ReceiverMessageSource()
        {
            _dataQueue = new BlockingCollection<DataMessage>(1 << 14);//CAPACITY?!
            _controlQueue = new BlockingCollection<ControlMessage>(1 << 14);

            _inputBuffer = new BlockingCollection<IMessage>();
            _receptionFlags = ReceptionFlags.Control | ReceptionFlags.Data; //TODO: even set flags on constuct?
            lockObj = new object();
        }

        #region Data message source
        
        DataMessage ISource<DataMessage>.Take(CancellationToken t)
        {
            return _dataQueue.Take(t);
        }

        Task ISource<DataMessage>.Flush()
        {
            _dataQueue.CompleteAdding();
            _dataQueue.Dispose();
            _dataQueue = new BlockingCollection<DataMessage>(1 << 14);
            return Task.CompletedTask;
        }
        #endregion

        #region Control message source

        ControlMessage ISource<ControlMessage>.Take(CancellationToken t)
        {
            return _controlQueue.Take(t);
        }

        Task ISource<ControlMessage>.Flush()
        {
            _controlQueue.CompleteAdding();
            _controlQueue.Dispose();
            _controlQueue = new BlockingCollection<ControlMessage>(1 << 14);
            return Task.CompletedTask;
        }
        #endregion

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
            if (!shouldBuffer && _inputBuffer.Any()) // flush blocked data input if receiving messages and there is any queued input
            {
                FlushInputBuffer();
            }
            SafeAddToInputQueue(message);
        }

        public void Flush()
        {
            //invoke after consumer is killed
            //assumption that producers keep running
            //may need to build in restart functionality in endpoints
            SetFlags(ReceptionFlags.Control);
        }

        public void SetFlags(ReceptionFlags mode)
        {
            _receptionFlags = mode;
        }

        public ReceptionFlags GetFlags()
        {
            return _receptionFlags;
        }

        /// <summary>
        /// Utility method that checks received message types to ensure early failure if an unexpected type is received
        /// </summary>
        /// <param name="message"></param>
        private void SafeAddToInputQueue(IMessage message)
        {
            if (message.IsControl)
            {
                _controlQueue.Add(message as ControlMessage ?? throw new Exception($"Unexpected control message type: {message.GetType()}"));
            }
            else
            {
                _dataQueue.Add(message as DataMessage ?? throw new Exception($"Unexpected data message type: {message.GetType()}"));
            }
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
        private void FlushInputBuffer()
        {
            lock (lockObj)
            {
                _inputBuffer.CompleteAdding();
                foreach (var blockedInput in _inputBuffer.GetConsumingEnumerable())
                {
                    SafeAddToInputQueue(blockedInput);
                }
                _inputBuffer = new BlockingCollection<IMessage>(1 << 14);
            }
        }
    }
}
