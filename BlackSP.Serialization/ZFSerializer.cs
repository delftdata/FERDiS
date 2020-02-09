using BlackSP.Serialization;
using System.IO;
using ZeroFormatter;

namespace BlackSP.Serialization
{
    public class ZFSerializer : BaseLengthPrefixedSerializer
    {

        public ZFSerializer() : base()
        {
        }

        protected sealed override T DoDeserialization<T>(byte[] input)
        {
            return ZeroFormatterSerializer.Deserialize<T>(input);
        }

        protected sealed override void DoSerialization<T>(Stream outputStream, T obj)
        {
            ZeroFormatterSerializer.Serialize(outputStream, obj);
        }
    }
}
