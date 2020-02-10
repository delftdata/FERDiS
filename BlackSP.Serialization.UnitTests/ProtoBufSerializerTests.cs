using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;
using BlackSP.Serialization;
using BlackSP.Serialization.UnitTests.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Serialization
{
    public class ProtoBufSerializerTests
    {
        ISerializer _serializer;
        ICollection<IEvent> _testEvents;
        CancellationTokenSource _ctSource;

        [SetUp]
        public void Setup()
        {
            var testComplexType = new Dictionary<string, int>();
            testComplexType.Add("entry", 0);
            testComplexType.Add("entry1", 1);
            testComplexType.Add("entry2", 2);

            _serializer = new ProtobufSerializer();

            _testEvents = new List<IEvent> {
                new ProtoBufTestEvent { Key = "test_key_0", Value = 420 },
                new ProtoBufTestEvent2 { Key = "test_key_02", Value = "420 Yo" },
                new ProtoBufTestEvent { Key = "test_key_1", Value = 420 },
                new ProtoBufTestEvent2 { Key = "test_key_12", Value = "420 Yo" }
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
                    ProtoBufTestEvent castedRes = res as ProtoBufTestEvent;
                    ProtoBufTestEvent2 castedRes2 = res as ProtoBufTestEvent2;
                    Assert.AreNotSame(castedRes == null, castedRes2 == null); //its only one of two types
                    Assert.AreEqual(res.Key, @event.Key, "Keys"); //keys are same from interface

                    if (castedRes != null) //its type 1 
                    {
                        Assert.AreEqual(((ProtoBufTestEvent)@event).Value, castedRes.Value);
                    }

                    if(castedRes2 != null) //its type 2
                    {
                        Assert.AreEqual(((ProtoBufTestEvent2)@event).Value, castedRes2.Value);
                    }

                    //Assert.AreEqual(castedRes.Value, ((ProtoBufTestEvent)@event).Value, "Values");
                }
            }
        }
    }
}