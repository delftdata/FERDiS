using BlackSP.Interfaces.Serialization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Serialization
{
    /// <summary>
    /// Abstraction layer that introduces reading and 
    /// writing to input/output stream while adding
    /// on a leading int32 to specify message length
    /// Designed to be used for (de)serializing from/to  
    /// network streams
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseLengthPrefixedSerializer : ISerializer
    {
        private readonly ArrayPool<byte> _arrayPool;

        public BaseLengthPrefixedSerializer()
        {
            _arrayPool = ArrayPool<byte>.Shared;
        }

        protected abstract void DoSerialization<T>(Stream outputStream, T obj);
        protected abstract T DoDeserialization<T>(byte[] input);

        public Task Serialize<T>(Stream outputStream, T obj)
        {
            using (Stream buffer = new MemoryStream())
            {
                //get obj bytes in buffer & reset stream position
                DoSerialization(buffer, obj);
                buffer.Seek(0, SeekOrigin.Begin);

                //get msg length in msg buffer
                byte[] buffLengthBytes = BitConverter.GetBytes((int)buffer.Length);

                outputStream.Write(buffLengthBytes, 0, buffLengthBytes.Length);
                buffer.CopyTo(outputStream);
            }
            return Task.CompletedTask;
        }


        /// <summary>
        /// Attempts to read the next T from the inputStream. Will
        /// return null when not enough bytes are buffered yet. The method
        /// is expected to be invoked again to retry.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public T Deserialize<T>(Stream inputStream, CancellationToken t)
        {
            try
            {
                int nextPackageByteLength = GetNextPackageLength(inputStream, t) ?? 0;
                if (nextPackageByteLength <= 0 || t.IsCancellationRequested)
                { return default; }

                return GetNextObject<T>(inputStream, nextPackageByteLength, t);
            }
            catch (ArgumentOutOfRangeException)
            {
                return default;
                //sometimes there are no bytes ready to be read from the underlying stream
                //just return null in this case and have caller try again.
            }
        }

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
        private int? GetNextPackageLength(Stream inputStream, CancellationToken t)
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

        private T GetNextObject<T>(Stream inputStream, int nextPackageByteLength, CancellationToken t)
        {
            int bytesReceivedCount = 0;
            byte[] nextMsgBytes = _arrayPool.Rent(nextPackageByteLength);
            T result;

            while (bytesReceivedCount < nextPackageByteLength)
            {
                if (t.IsCancellationRequested)
                { return default; }

                int bytesRead = inputStream.Read(nextMsgBytes, bytesReceivedCount, nextPackageByteLength - bytesReceivedCount);
                bytesReceivedCount += bytesRead;
            }

            result = DoDeserialization<T>(nextMsgBytes);

            _arrayPool.Return(nextMsgBytes);
            return result;
        }
    }
}
