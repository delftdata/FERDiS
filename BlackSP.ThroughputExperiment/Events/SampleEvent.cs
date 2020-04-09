﻿using BlackSP.Kernel.Events;
using ProtoBuf;
using System;

namespace BlackSP.ThroughputExperiment.Events
{
    [ProtoContract]
    public class SampleEvent : IEvent
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public string Value { get; set; }

        public SampleEvent(string key, DateTime? eventTime, string value)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}