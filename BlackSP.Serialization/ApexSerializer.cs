using Apex.Serialization;
using System.IO;

namespace BlackSP.Serialization
{
    public class ApexSerializer : BaseLengthPrefixedSerializer
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
