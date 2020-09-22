using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Serialization
{
    public class PooledBufferMessageSerializer : IObjectSerializer
    {

        private readonly IStreamSerializer _serializer;
        private readonly RecyclableMemoryStreamManager _msgBufferPool;

        public PooledBufferMessageSerializer(IStreamSerializer serializer,
                                 RecyclableMemoryStreamManager memStreamPool)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _msgBufferPool = memStreamPool ?? throw new ArgumentNullException(nameof(memStreamPool));
        }

        public async Task<byte[]> SerializeAsync<T>(T message, CancellationToken t)
        {
            var msgBuffer = _msgBufferPool.GetStream();
            await _serializer.Serialize(msgBuffer, message).ConfigureAwait(false);
            byte[] msgBytes = msgBuffer.ToArray();
            msgBuffer.Dispose();
            return msgBytes;
        }

        public async Task<T> DeserializeAsync<T>(byte[] msgBytes, CancellationToken t)
        {
            var msgStream = new MemoryStream(msgBytes);
            try
            {
                T nextMessage = await _serializer.Deserialize<T>(msgStream, t).ConfigureAwait(false);
                if (nextMessage == null)
                {
                    throw new SerializationException($"Message deserialization returned null");
                }
                return nextMessage;
            }
            finally
            {
                msgStream.Dispose();
            }
        }
    }
}
