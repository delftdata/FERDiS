using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IMessage
    {
        bool IsControl { get; }

        int PartitionKey { get; }

        /// <summary>
        /// Try to get a strongly typed MessagePayloadBase implementation<br/>
        /// Should return null if no such payload type is present in the message
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="payload"></param>
        /// <returns></returns>
        bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : MessagePayloadBase;

        /// <summary>
        /// Adds a payload to the message. Any existing payload with the same TPayload should be overwritten.
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="payload"></param>
        void AddPayload<TPayload>(TPayload payload) where TPayload : MessagePayloadBase;
    }

    public abstract class MessagePayloadBase
    {
        public static string MetaDataKey => throw new NotSupportedException();
    }
}
