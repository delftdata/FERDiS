using BlackSP.Serialization;
using BlackSP.Serialization.Events;
using BlackSP.Serialization.Serializers;
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
            Formatter.AppendDynamicUnionResolver(RegisterZeroFormatterEventTypes);
        }

        protected sealed override T DoDeserialization<T>(byte[] input)
        {
            return ZeroFormatterSerializer.Deserialize<T>(input);
        }

        protected sealed override void DoSerialization<T>(Stream outputStream, T obj)
        {
            ZeroFormatterSerializer.Serialize(outputStream, obj);
        }

        /// <summary>
        /// Registers all event classes that have been loaded into the runtime that extend the
        /// BaseZeroFormattableEvent, required to allow zeroformatter to distinguish between
        /// these events
        /// </summary>
        /// <param name="unionType"></param>
        /// <param name="resolver"></param>
        private void RegisterZeroFormatterEventTypes(Type unionType, DynamicUnionResolver resolver)
        {
            var baseEventType = typeof(BaseZeroFormattableEvent);
            if (unionType != baseEventType)
            { return; }

            resolver.RegisterUnionKeyType(typeof(byte));
            var eventTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => baseEventType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract && !p.IsInterface);

            byte unionKey = 0;
            foreach (Type eventType in eventTypes)
            {
                resolver.RegisterSubType(unionKey, eventType);
                unionKey++;
            }
        }
    }
}
