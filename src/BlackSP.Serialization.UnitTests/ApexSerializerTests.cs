﻿using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Apex;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Serialization.UnitTests.Serialization
{
    public class ApexSerializerTests
    {
        IStreamSerializer _serializer;
        ICollection<IEvent> _testEvents;
        CancellationTokenSource _ctSource;

        [SetUp]
        public void Setup()
        {
            var testComplexType = new Dictionary<string, int>();
            testComplexType.Add("entry", 0);
            testComplexType.Add("entry1", 1);
            testComplexType.Add("entry2", 2);

            _serializer = new ApexSerializer();
            _testEvents = new List<IEvent> {
                new TestEvent(0, testComplexType),
                new TestEvent(1, testComplexType),
                new TestEvent(2, testComplexType),
            };
            _ctSource = new CancellationTokenSource();
        }

        [Test]
        public async Task SerializeAndDeserializeAreCompatible()
        {            
            using(Stream serializeBuffer = new MemoryStream())
            {
                //write multiple events to stream
                var eventEnumerator = _testEvents.GetEnumerator();
                while (eventEnumerator.MoveNext())
                {
                    IEvent @event = eventEnumerator.Current;
                    await _serializer.Serialize(serializeBuffer, @event);
                }

                //seek back to beginning of stream
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //read events from stream
                foreach (var @event in _testEvents)
                {
                    IEvent res = await _serializer.Deserialize<IEvent>(serializeBuffer, _ctSource.Token);
                    TestEvent castedRes = res as TestEvent;
                    Assert.IsNotNull(castedRes);
                    Assert.AreEqual(res.Key, @event.Key, "Keys");
                    Assert.AreEqual(castedRes.GetValue(), ((TestEvent)@event).GetValue(), "Values");
                }
            }
        }
        private class TestEvent : IEvent
        {
            public int? Key { get; set; }

            public DateTime EventTime { get; set; }

            private IDictionary<string, int> _values;

            public TestEvent(int key, IDictionary<string, int> values)
            {
                Key = key;
                _values = values;
            }

            public object GetValue()
            {
                return _values;
            }

            public int GetPartitionKey()
            {
                return Key.GetHashCode();
            }
        }
    }
}