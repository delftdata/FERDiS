using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Serializers;
using BlackSP.Serialization.Utilities;
using ProtoBuf;
using ProtoBuf.Meta;

namespace BlackSP.Serialization.Serializers
{
    public class ProtobufSerializer : ISerializer
    {
        private TypeModel _protobuf;


        public ProtobufSerializer()
        {
            var inheritanceFieldNum = 64; //set high to not get in the way of individual model definitions
            
            
            _protobuf = BuildTypeModel(inheritanceFieldNum);
        }

        public Task<T> Deserialize<T>(Stream inputStream, CancellationToken t)
        {
            return Task.FromResult(
                (T)_protobuf.Deserialize(inputStream, null, typeof(T), inputStream.Length)
            );

        }

        public Task Serialize<T>(Stream outputStream, T obj)
        {
            _protobuf.Serialize(outputStream, obj);
            return Task.CompletedTask;
        }

        private TypeModel BuildTypeModel(int inheritanceFieldNum)
        {
            var typeModel = RuntimeTypeModel.Create();
            var baseEventType = typeModel.Add(typeof(IEvent), true);
            var subTypes = TypeLoader.GetClassesExtending(typeof(IEvent), false);
            foreach (var subType in subTypes)
            {
                baseEventType.AddSubType(inheritanceFieldNum++, subType);
            }

            var baseMessageType = typeModel.Add(typeof(IMessage), true);
            var msgSubTypes = TypeLoader.GetClassesExtending(typeof(IMessage), false);
            foreach (var subType in msgSubTypes)
            {
                baseMessageType.AddSubType(inheritanceFieldNum++, subType);
            }

            return typeModel.Compile();
        }
    }
}
