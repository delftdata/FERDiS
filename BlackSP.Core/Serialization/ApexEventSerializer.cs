using Apex.Serialization;
using BlackSP.Core.Events;
using System;
using System.IO;
using System.Threading;

namespace BlackSP.Core.Serialization
{
    public class ApexEventSerializer : IEventSerializer
    {
        IBinary _apexSerializer;

        public ApexEventSerializer(IBinary apexSerializer)
        {
            _apexSerializer = apexSerializer;
        }

        public void SerializeEvent(Stream outputStream, ref IEvent @event)
        {
            using (Stream apexBuffer = new MemoryStream())
            {
                //get apex bytes in buffer (+seek back to start)
                _apexSerializer.Write(@event, apexBuffer);
                apexBuffer.Seek(0, SeekOrigin.Begin);
                //get msg length in msg buffer
                byte[] buffLengthBytes = BitConverter.GetBytes((int)apexBuffer.Length);
                //declare byte buffer large enough for the message
                byte[] msgBytes = new byte[buffLengthBytes.Length + apexBuffer.Length];
                //first copy in message length bytes
                Array.Copy(buffLengthBytes, 0, msgBytes, 0, buffLengthBytes.Length);
                //then append apex bytes
                apexBuffer.Read(msgBytes, buffLengthBytes.Length, (int)apexBuffer.Length);
                //finally write msgBytes to buffer
                outputStream.Write(msgBytes, 0, msgBytes.Length);
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
            byte[] nextMsgBytes = new byte[4];
            while (bytesReceivedCount < nextMsgBytes.Length)
            {
                if (t.IsCancellationRequested)
                { return null; }
                
                int bytesRead = inputStream.Read(nextMsgBytes, bytesReceivedCount, nextMsgBytes.Length - bytesReceivedCount);
                bytesReceivedCount += bytesRead;
            }

            return BitConverter.ToInt32(nextMsgBytes, 0);
        }

        private IEvent GetNextEvent(Stream inputStream, int nextEventByteLength, CancellationToken t)
        {
            int bytesReceivedCount = 0;
            byte[] nextMsgBytes = new byte[nextEventByteLength];
            using (Stream buffer = new MemoryStream(nextEventByteLength))
            {
                while (bytesReceivedCount < nextEventByteLength)
                {
                    if (t.IsCancellationRequested)
                    { return null; }

                    int bytesRead = inputStream.Read(nextMsgBytes, bytesReceivedCount, nextMsgBytes.Length);
                    bytesReceivedCount += bytesRead;
                    if (bytesRead > 0)
                    {
                        buffer.Write(nextMsgBytes, 0, nextMsgBytes.Length);
                    }
                }

                buffer.Seek(0, SeekOrigin.Begin);
                return _apexSerializer.Read<IEvent>(buffer);
            }
        }
        #endregion
    
    }
}
