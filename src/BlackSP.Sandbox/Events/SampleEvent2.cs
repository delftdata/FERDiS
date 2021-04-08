using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.Sandbox.Events
{
    [ProtoContract]
    public class SampleEvent2 : IEvent
    {
        [ProtoMember(1)]
        public int? Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public int EventCount { get; set; }

        public SampleEvent2() { }

        public SampleEvent2(int? key, DateTime? eventTime, int count)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            EventCount = count;
        }

    }
}
