using BlackSP.Serialization;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.HighPerformance.Extensions;

namespace BlackSP.Benchmarks.Kafka
{
    public class ProtoBufAsyncValueSerializer<T> : IAsyncSerializer<T>, IAsyncDeserializer<T>
        where T : class
    {

        private readonly ProtobufStreamSerializer _serializer;

        public ProtoBufAsyncValueSerializer() {
            _serializer = new ProtobufStreamSerializer();
        }

        public async Task<T> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
        {
            if(isNull) { return null; }
            return await _serializer.Deserialize<T>(data.AsStream(), default);
        }

        public async Task<byte[]> SerializeAsync(T data, SerializationContext context)
        {
            using var memstream = new MemoryStream();
            await _serializer.Serialize(memstream, data);
            return memstream.ToArray();
        }
    }
}
