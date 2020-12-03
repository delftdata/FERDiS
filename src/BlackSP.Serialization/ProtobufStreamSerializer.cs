using System;
using System.IO;
using System.Linq;
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
            _protobuf = BuildTypeModel();
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

        private TypeModel BuildTypeModel()
        {
            int inheritanceFieldNum = 64; //set high to not get in the way of individual model definitions

            var typeModel = RuntimeTypeModel.Create();
            foreach(var type in TypeLoader.GetProtobufAnnotatedTypes())
            {
                var interfaces = type.GetInterfaces();
                foreach (var interf in interfaces)
                {
                    //Console.WriteLine($"Type {type} under interface {interf}");
                    var metaType = typeModel.Add(interf, true);
                    metaType.AddSubType(inheritanceFieldNum++, type);
                }

                if(!interfaces.Any())
                {
                    var baseType = type.GetHighestBaseType();
                    if (baseType != type)
                    {
                        //Console.WriteLine($"Type {type} under base {baseType}");
                        var metaType = typeModel.Add(baseType, true);
                        metaType.AddSubType(inheritanceFieldNum++, type);
                    }
                    else
                    {
                        //Console.WriteLine($"Type {type} standalone");
                        typeModel.Add(baseType, true);
                    }
                }
            }

            return typeModel.Compile();
        }

        /**
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
         */
    }
}
