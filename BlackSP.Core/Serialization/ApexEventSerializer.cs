using Apex.Serialization;
using BlackSP.Core.Events;
using System;
using System.Buffers;
using System.IO;
using System.Threading;

namespace BlackSP.Core.Serialization
{
    public class ApexEventSerializer : IEventSerializer
    {
        private readonly IBinary _apexSerializer;
        private readonly ArrayPool<byte> _arrayPool;

        public ApexEventSerializer()
        {
            _apexSerializer = Binary.Create();
            _arrayPool = ArrayPool<byte>.Shared;
        }

        public ApexEventSerializer(IBinary apexSerializer)
        {
            _apexSerializer = apexSerializer;
            _arrayPool = ArrayPool<byte>.Shared;
        }

        public void SerializeEvent(Stream outputStream, IEvent @event)
        {
            using (Stream apexBuffer = new MemoryStream())
            {
                //get apex bytes in buffer & reset stream position
                _apexSerializer.Write(@event, apexBuffer);
                apexBuffer.Seek(0, SeekOrigin.Begin);
                
                //get msg length in msg buffer
                byte[] buffLengthBytes = BitConverter.GetBytes((int)apexBuffer.Length);
                
                outputStream.Write(buffLengthBytes, 0, buffLengthBytes.Length);
                apexBuffer.CopyTo(outputStream);
            }
        }

        /// <summary>
        /// Attempts to read the next IEvent from the inputStream. Will
        /// return null when not enough bytes are buffered yet. The method
        /// is expected to be invoked again to retry.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public IEvent DeserializeEvent(Stream inputStream, CancellationToken t)
        {
            try
            {
                int nextEventByteLength = GetNextEventLength(inputStream, t) ?? 0;
                if (nextEventByteLength <= 0 || t.IsCancellationRequested)
                { return null; }

                return GetNextEvent(inputStream, nextEventByteLength, t);
            } 
            catch(ArgumentOutOfRangeException e)
            {
                return null; 
                //sometimes there are no bytes ready to be read from the underlying stream
                //just return null in this case and have caller try again.
            }
        }

        #region private helper methods

        /// <summary>
        /// Tries to read the first 4 bytes of the stream and
        /// interprets them as an int32 indicating the lenght 
        /// of the next incoming message. <br/>
        /// Note: may throw ArgumentOutOfRangeException when
        /// less than 4 bytes are buffered in the underlying
        /// stream.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private int? GetNextEventLength(Stream inputStream, CancellationToken t)
        {
            int bytesReceivedCount = 0;
            int bytesToRead = 4; //4 bytes to represent an int32
            byte[] nextMsgBytes = _arrayPool.Rent(bytesToRead);
            while (bytesReceivedCount < bytesToRead)
            {
                if (t.IsCancellationRequested)
                { return null; }
                
                int bytesRead = inputStream.Read(nextMsgBytes, bytesReceivedCount, bytesToRead - bytesReceivedCount);
                bytesReceivedCount += bytesRead;
            }
            int receivedInt = BitConverter.ToInt32(nextMsgBytes, 0);
            _arrayPool.Return(nextMsgBytes);
            return receivedInt;
        }

        private IEvent GetNextEvent(Stream inputStream, int nextEventByteLength, CancellationToken t)
        {
            int bytesReceivedCount = 0;
            byte[] nextMsgBytes = _arrayPool.Rent(nextEventByteLength);
            IEvent result;

            while (bytesReceivedCount < nextEventByteLength)
            {
                if (t.IsCancellationRequested)
                { return null; }

                int bytesRead = inputStream.Read(nextMsgBytes, bytesReceivedCount, nextEventByteLength - bytesReceivedCount);
                bytesReceivedCount += bytesRead;
            }

            using (Stream buffer = new MemoryStream(nextMsgBytes))
            {
                result = _apexSerializer.Read<IEvent>(buffer);
            }
            _arrayPool.Return(nextMsgBytes);
            return result;
        }
        #endregion
    
    }
}
