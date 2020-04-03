using BlackSP.Kernel.Events;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Serializers;
using BlackSP.Serialization.UnitTests.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                new ProtoBufTestEvent("test_key_0", DateTime.Now, 420),
                new ProtoBufTestEvent2("test_key_1", DateTime.Now, "yeah 420"),
                new ProtoBufTestEvent("test_key_2", DateTime.Now, 420),
                new ProtoBufTestEvent2("test_key_3", DateTime.Now, "yeah 420")
            };

            _ctSource = new CancellationTokenSource();
        }

        [Test]
        public async Task SerializeAndDeserializeAreCompatible()
        {   //important property of this test: serialization should 
            //support not caring about underlying event type     
            var eventEnumerator = _testEvents.GetEnumerator();
            while (eventEnumerator.MoveNext())
            {
                using (Stream serializeBuffer = new MemoryStream())
                {
                    IEvent @event = eventEnumerator.Current;
                    await _serializer.Serialize(serializeBuffer, @event);
                    serializeBuffer.Seek(0, SeekOrigin.Begin);
                    IEvent res = await _serializer.Deserialize<IEvent>(serializeBuffer, _ctSource.Token);
                    ProtobufResultAssertions(@event, res);
                }
            }
        }

        private void ProtobufResultAssertions(IEvent expected, IEvent result)
        {
            Assert.AreEqual(expected.Key, result.Key, "Keys"); //keys are same from interface

            ProtoBufTestEvent castedRes = result as ProtoBufTestEvent;
            ProtoBufTestEvent2 castedRes2 = result as ProtoBufTestEvent2;
            Assert.AreNotSame(castedRes == null, castedRes2 == null); //its only one of two types

            if (castedRes != null) //its type 1 
            {
                Assert.AreEqual(((ProtoBufTestEvent)result).Value, castedRes.Value);
            }

            if (castedRes2 != null) //its type 2
            {
                Assert.AreEqual(((ProtoBufTestEvent2)result).Value, castedRes2.Value);
            }
        }
    }
}