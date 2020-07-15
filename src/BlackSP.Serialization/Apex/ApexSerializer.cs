using Apex.Serialization;
using System;
using System.IO;

namespace BlackSP.Serialization.Apex
{
    [Obsolete("No longer serves any purpose in BlackSP")]
    public class ApexSerializer : LengthPrefixedSerializerBase
    {
        private readonly IBinary _apexSerializer;

        public ApexSerializer() : base()
        {
            _apexSerializer = Binary.Create();
        }

        public ApexSerializer(IBinary apexSerializer) : base()
        {
            _apexSerializer = apexSerializer;
        }

        protected override void DoSerialization<T>(Stream outputStream, T obj)
        {
            _apexSerializer.Write(obj, outputStream);
        }

        protected override T DoDeserialization<T>(byte[] input)
        {
            using (Stream buffer = new MemoryStream(input))
            {
                return _apexSerializer.Read<T>(buffer);
            }
        }

    }
}
