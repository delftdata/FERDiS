using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;
using BlackSP.Serialization;
using BlackSP.Serialization.Events;
using BlackSP.Serialization.UnitTests.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZeroFormatter;

namespace BlackSP.Core.UnitTests.Serialization
{
    public class ZeroFormatterSerializerTests
    {
        ISerializer _serializer;
        ICollection<BaseZeroFormattableEvent> _testEvents;
        CancellationTokenSource _ctSource;

        [SetUp]
        public void Setup()
        {
            var testDict = new Dictionary<string, int>();
            testDict.Add("entry", 0);
            testDict.Add("entry1", 1);
            testDict.Add("entry2", 2);

            var testArray = new int[] { 1, 2, 3, 4, 5 };

            ZFSerializer.RegisterTypes();
            _serializer = new ZFSerializer();
            
            _testEvents = new List<BaseZeroFormattableEvent> {
                new ZeroFormatterTestEvent{ Key = "test_key_0", Values = testDict },
                new ZeroFormatterTestEvent2{ Key = "test_key_1", Values = testArray },
                new ZeroFormatterTestEvent{ Key = "test_key_2", Values = testDict },
            };
            _ctSource = new CancellationTokenSource();
        }

        [Test]
        public async Task SerializeAndDeserializeAreCompatible()
        {
            using (Stream serializeBuffer = new MemoryStream())
            {
                //serialize multiple events to stream
                var eventEnumerator = _testEvents.GetEnumerator();
                while (eventEnumerator.MoveNext())
                {
                    BaseZeroFormattableEvent @event = eventEnumerator.Current;
                    await _serializer.Serialize(serializeBuffer, @event);
                }
                //seek back to beginning of stream
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //seek back to beginning of stream
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //deserialize events from stream
                foreach (var @event in _testEvents)
                {
                    IEvent res = await _serializer.Deserialize<BaseZeroFormattableEvent>(serializeBuffer, _ctSource.Token);
                    Assert.IsNotNull(res);
                    //Assert key value is as expected
                    Assert.AreEqual(@event.Key, res.Key, "Keys");
                    //Assert event is one of expected types
                    Assert.AreNotEqual(res as ZeroFormatterTestEvent, res as ZeroFormatterTestEvent2);
                    //Assert class specific properties are available and correct
                    if (res as ZeroFormatterTestEvent == null)
                    {
                        Assert.AreEqual(((ZeroFormatterTestEvent2)@event).Values, ((ZeroFormatterTestEvent2)@event).Values);
                    }
                    else
                    {
                        Assert.AreEqual(((ZeroFormatterTestEvent)@event).Values, ((ZeroFormatterTestEvent)@event).Values);
                    }
                }
            }
        }

        [Test]
        public async Task SerializeAndDeserializeAreCompatibleInMultipleIterations()
        {
            /*using (Stream serializeBuffer = new MemoryStream())
            {
                //serialize multiple events to stream
                var eventEnumerator = _testEvents.GetEnumerator();
                while (eventEnumerator.MoveNext())
                {
                    BaseZeroFormattableEvent @event = eventEnumerator.Current;
                    await _serializer.Serialize(serializeBuffer, @event);
                }
                //seek back to beginning of stream
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //deserialize all events back out
                List<BaseZeroFormattableEvent> events = new List<BaseZeroFormattableEvent>();
                foreach(var _ in _testEvents)
                {
                    events.Add(await _serializer.Deserialize<BaseZeroFormattableEvent>(serializeBuffer, _ctSource.Token));
                }

                serializeBuffer.Flush();
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //and serialize them again to test if the ZF Proxy class also properly gets serialized
                eventEnumerator = events.GetEnumerator();
                while (eventEnumerator.MoveNext())
                {
                    BaseZeroFormattableEvent @event = eventEnumerator.Current;
                    await _serializer.Serialize(serializeBuffer, @event);
                }
                //seek back to beginning of stream
                serializeBuffer.Seek(0, SeekOrigin.Begin);

                //deserialize events from stream
                foreach (var @event in _testEvents)
                {
                    IEvent res = await _serializer.Deserialize<BaseZeroFormattableEvent>(serializeBuffer, _ctSource.Token);
                    Assert.IsNotNull(res);
                    //Assert key value is as expected
                    Assert.AreEqual(@event.Key, res.Key, "Keys");
                    //Assert event is one of expected types
                    Assert.AreNotEqual(res as ZeroFormatterTestEvent, res as ZeroFormatterTestEvent2);
                    //Assert class specific properties are available and correct
                    if (res as ZeroFormatterTestEvent == null)
                    {
                        Assert.AreEqual(((ZeroFormatterTestEvent2)@event).Values, ((ZeroFormatterTestEvent2)@event).Values);
                    } else
                    {
                        Assert.AreEqual(((ZeroFormatterTestEvent)@event).Values, ((ZeroFormatterTestEvent)@event).Values);
                    }
                }
            }*/
            Assert.Fail();
        }
    }
}