using Apex.Serialization;
using BlackSP.Core.Events;
using System;
using System.Buffers;
using System.IO;
using System.Threading;

namespace BlackSP.Core.Serialization
{
    public class NoOpEventSerializer : IEventSerializer
    {
        private readonly ArrayPool<byte> _arrayPool;

        public NoOpEventSerializer()
        {
            _arrayPool = ArrayPool<byte>.Shared;
        }


        public void SerializeEvent(Stream outputStream, IEvent @event)
        {
            using (Stream apexBuffer = new MemoryStream())
            {
                var bytes = BitConverter.GetBytes(0);
                apexBuffer.Write(bytes, 0, 4);
                
                //_apexSerializer.Write(@event, apexBuffer);
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
                int nextEventByteLength = 4;
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

            result = null;

            _arrayPool.Return(nextMsgBytes);
            return result;
        }
        #endregion
    
    }
}
