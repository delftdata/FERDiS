using System.IO;
using BlackSP.Serialization.Serializers;

namespace BlackSP.Serialization.Serializers
{
    public class NoOpEventSerializer : LengthPrefixedSerializerBase
    {

        public NoOpEventSerializer() : base()
        {
        }

        protected override T DoDeserialization<T>(byte[] input)
        {
            return default;
        }

        protected override void DoSerialization<T>(Stream outputStream, T obj)
        {
            outputStream.Write(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 0, 10);
        }
    }
}
