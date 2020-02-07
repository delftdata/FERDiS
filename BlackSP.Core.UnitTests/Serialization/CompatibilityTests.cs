using Apex.Serialization;
using BlackSP.Core.Events;
using BlackSP.Core.Serialization;
using BlackSP.Core.UnitTests.Events;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Serialization
{
    public class CompatibilityTests
    {
        IEventSerializer _serializer;
        ICollection<IEvent> _testEvents;
        CancellationTokenSource _ctSource;

        [SetUp]
        public void Setup()
        {
            _serializer = new ApexEventSerializer(Binary.Create());
            _testEvents = new List<IEvent> {
                new TestEvent("test_key_0", 1337),
                new TestEvent("test_key_1", 1338),
                new TestEvent("test_key_2", 1339),
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
                    _serializer.SerializeEvent(serializeBuffer, ref @event);
                }

                //seek back to beginning of stream
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //read events from stream
                foreach (var @event in _testEvents)
                {
                    IEvent res = _serializer.DeserializeEvent(serializeBuffer, _ctSource.Token);
                    Assert.IsNotNull(res);
                    Assert.AreEqual(res.Key, @event.Key, "Keys");
                    Assert.AreEqual(res.GetValue(), @event.GetValue(), "Values");
                }
            }
        }
    }
}
