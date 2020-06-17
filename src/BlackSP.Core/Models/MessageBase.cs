using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models
{
    public abstract class MessageBase : IMessage
    {
        public abstract bool IsControl { get; }

        public abstract int PartitionKey { get; }

        public abstract IDictionary<string, MessagePayloadBase> MetaData { get; }

        public bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : MessagePayloadBase
        {
            var metadataKey = typeof(TPayload).GetProperty(nameof(MessagePayloadBase.MetaDataKey))?.GetValue(null) as string ?? null;
            if (string.IsNullOrEmpty(metadataKey))
            {
                throw new ArgumentException($"Payload type \"{typeof(TPayload)}\" does not implement static string MetaDataKey property", nameof(payload));
            }
            if (MetaData.TryGetValue(metadataKey, out MessagePayloadBase payloadBase))
            {
                payload = payloadBase as TPayload;
                return true;
            }
            payload = null;
            return false;
        }

        public void AddPayload<TPayload>(TPayload payload) where TPayload : MessagePayloadBase
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            var metaDataKey = typeof(TPayload).GetProperty(nameof(MessagePayloadBase.MetaDataKey))?.GetValue(null) as string ?? null;
            if (string.IsNullOrEmpty(metaDataKey))
            {
                throw new ArgumentException("Payload type does not implement static string MetaDataKey property", nameof(TPayload));
            }

            if (MetaData.ContainsKey(metaDataKey))
            {
                MetaData.Remove(metaDataKey);
            }
            MetaData.Add(metaDataKey, payload);
        }
    }
}
