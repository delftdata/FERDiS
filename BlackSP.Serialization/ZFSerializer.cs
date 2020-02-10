using BlackSP.Serialization;
using BlackSP.Serialization.Events;
using BlackSP.Serialization.Serializers;
using BlackSP.Serialization.Utilities;
using System;
using System.IO;
using System.Linq;
using ZeroFormatter;
using ZeroFormatter.Formatters;

namespace BlackSP.Serialization
{
    public class ZFSerializer : BaseLengthPrefixedSerializer
    {

        public ZFSerializer() : base()
        {
        }

        public static void RegisterTypes()
        {
            Formatter.AppendDynamicUnionResolver(RegisterZeroFormatterEventTypes);
        }

        protected sealed override T DoDeserialization<T>(byte[] input)
        {
            try
            {
                return ZeroFormatterSerializer.Deserialize<T>(input);
            } catch(Exception e)
            {
                Console.WriteLine("ZF Deserialization Err");
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        protected sealed override void DoSerialization<T>(Stream outputStream, T obj)
        {
            try
            {
                //Console.WriteLine("Gonna serialize.. " + obj.GetType().FullName + ", "+obj.ToString());
                ZeroFormatterSerializer.Serialize(outputStream, obj);
            } catch (Exception e)
            {
                Console.WriteLine("ZF Serialization Err");
                Console.WriteLine(obj.ToString());
                Console.WriteLine(e.ToString());
                throw;
            }

        }

        /// <summary>
        /// Registers all event classes that have been loaded into the runtime that extend the
        /// BaseZeroFormattableEvent, required to allow zeroformatter to distinguish between
        /// these events
        /// </summary>
        /// <param name="unionType"></param>
        /// <param name="resolver"></param>
        private static void RegisterZeroFormatterEventTypes(Type unionType, DynamicUnionResolver resolver)
        {
            var baseEventType = typeof(BaseZeroFormattableEvent);
            if (unionType != baseEventType)
            { return; }

            var eventTypes = TypeLoader.GetClassesExtending(baseEventType);
            resolver.RegisterUnionKeyType(typeof(byte));
            byte unionKey = 0;
            foreach (Type eventType in eventTypes)
            {
                resolver.RegisterSubType(unionKey, eventType);
                unionKey++;
            }
        }
    }
}
