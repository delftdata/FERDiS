using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests.Models
{
    class TestMessage : IMessage
    {
        public bool IsControl { get; set; }

        public int? PartitionKey { get; set; }

        public IEnumerable<MessagePayloadBase> Payloads { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public (IEndpointConfiguration, int)? TargetOverride { get; set; }

        public void AddPayload<TPayload>(TPayload payload) where TPayload : MessagePayloadBase
        {
        }

        public bool TryExtractPayload<TPayload>(out TPayload payload) where TPayload : MessagePayloadBase
        {
            payload = default;
            return false;
        }
    }
}
