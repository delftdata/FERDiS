using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;
using BlackSP.Serialization.Serializers;
using BlackSP.Serialization.Utilities;
using ProtoBuf;
using ProtoBuf.Meta;

namespace BlackSP.Serialization
{
    public class ProtobufSerializer : ISerializer //BaseLengthPrefixedSerializer
    {
        private TypeModel _protobuf;
        private readonly PrefixStyle _prefixStyle;
        private readonly int _inheritanceFieldNum;


        public ProtobufSerializer()
        {
            _inheritanceFieldNum = 63; //set high to not get in the way of individual model definitions
            _prefixStyle = PrefixStyle.Fixed32;
            
            var typeModel = RuntimeTypeModel.Create();
            var baseEventType = typeModel.Add(typeof(IEvent), true);
            var subTypes = TypeLoader.GetClassesExtending(typeof(IEvent), false);
            foreach(var subType in subTypes)
            {
                baseEventType.AddSubType(_inheritanceFieldNum++, subType);
            }
            _protobuf = typeModel.Compile();
        }

        public Task<T> Deserialize<T>(Stream inputStream, CancellationToken t)
        {
            return Task.FromResult(
                (T)_protobuf.DeserializeWithLengthPrefix(inputStream, null, typeof(T), _prefixStyle, 0)
            );
        }

        public Task Serialize<T>(Stream outputStream, T obj)
        {
            _protobuf.SerializeWithLengthPrefix(outputStream, obj, typeof(T), _prefixStyle, 0);
            return Task.CompletedTask;
        }

        /*
        protected override T DoDeserialization<T>(byte[] input)
        {
            _protobuf.DeserializeWithLengthPrefix()
            return default;
        }

        protected override void DoSerialization<T>(Stream outputStream, T obj)
        {
            outputStream.Write(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 0, 10);
        }
        */
    }
}
