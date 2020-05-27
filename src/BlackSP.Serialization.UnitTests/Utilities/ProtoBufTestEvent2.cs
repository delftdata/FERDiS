﻿using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Kernel.Events;
using ProtoBuf;
namespace BlackSP.Serialization.UnitTests.Utilities
{
    [ProtoContract]
    public class ProtoBufTestEvent2 : IEvent
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public string Value { get; set; }

        public ProtoBufTestEvent2() : base()
        {

        }

        public ProtoBufTestEvent2(string key, DateTime? eventTime, string value2)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            Value = value2;
        }
    }
}