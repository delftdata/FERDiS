using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.Sandbox.Events
{
    [ProtoContract]
    public class SampleEvent : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public string Value { get; set; }

        public SampleEvent() { }

        public SampleEvent(int key, DateTime? eventTime, string value)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
