using BlackSP.Kernel.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IMessage
    {
        bool IsControl { get; }

        int? PartitionKey { get; }

        IEnumerable<MessagePayloadBase> Payloads { get; }

        DateTime CreatedAtUtc { get; }

        /// <summary>
        /// Utility property allowing the overriding of target connection where the message will be sent<br/>
        /// Process-local property (i.e. should not be sent along with the message)
        /// </summary>
        (IEndpointConfiguration, int)? TargetOverride { get; set; }

        /// <summary>
        /// Try to extract a strongly typed MessagePayloadBase implementation<br/>
        /// Should remove the payload from the message if it is present<br/>
        /// Should return false/null if no such payload type is present in the message
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="payload"></param>
        /// <returns></returns>
        bool TryExtractPayload<TPayload>(out TPayload payload) where TPayload : MessagePayloadBase;

        /// <summary>
        /// Adds a payload to the message. Any existing payload with the same TPayload should be overwritten.
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="payload"></param>
        void AddPayload<TPayload>(TPayload payload) where TPayload : MessagePayloadBase;
    }

    [Serializable]
    public abstract class MessagePayloadBase
    {
        public static string MetaDataKey => throw new NotImplementedException();
    }
}
