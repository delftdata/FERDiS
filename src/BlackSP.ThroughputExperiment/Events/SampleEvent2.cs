using BlackSP.Kernel.Models;
using ProtoBuf;
using System;

namespace BlackSP.ThroughputExperiment.Events
{
    [ProtoContract]
    public class SampleEvent2 : IEvent
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public string Value2 { get; set; }

        public SampleEvent2() { }

        public SampleEvent2(string key, DateTime? eventTime, string value2)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            Value2 = value2 ?? throw new ArgumentNullException(nameof(value2));
        }

        public int GetPartitionKey()
        {
            return Key.GetHashCode();
        }
    }
}
