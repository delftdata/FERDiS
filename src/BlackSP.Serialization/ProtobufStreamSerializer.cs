using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Utilities;
using ProtoBuf;
using ProtoBuf.Meta;

namespace BlackSP.Serialization
{
    /// <summary>
    /// Builds a type model based on any and all protobuf annotated classes it can find in the runtime it starts in.
    /// </summary>
    public class ProtobufStreamSerializer : IStreamSerializer
    {
        private TypeModel _protobuf;


        public ProtobufStreamSerializer()
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

            var basePayloadType = typeModel.Add(typeof(MessagePayloadBase), true);
            var payloadSubTypes = TypeLoader.GetClassesExtending(typeof(MessagePayloadBase), false);
            foreach (var subType in payloadSubTypes)
            {
                basePayloadType.AddSubType(inheritanceFieldNum++, subType);
            }

            return typeModel.Compile();
        }
    }
}
