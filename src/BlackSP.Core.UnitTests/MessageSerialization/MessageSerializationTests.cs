using BlackSP.Core.Extensions;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Models;
using BlackSP.Serialization.Serializers;
using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.MessageSerialization
{
    
    [ProtoContract]
    public class VectorClockMessagePayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:vector-clock";

        [ProtoMember(1)]
        public int SeqNr { get; set; }

        [ProtoMember(2)]
        public string Smth { get; set; }

        [ProtoMember(3)]
        public int[] SeqArr { get; set; }
    }

    public class MessageSerializationTests
    {
        [Test]
        public async Task ControlMessageSerializes()
        {
            MessageBase msg = new ControlMessage();
            msg.AddPayload(new EventPayload() { Event = new TestEvent() { Key = "k", Value = 1 } });
            msg.AddPayload(new VectorClockMessagePayload() { SeqNr = 111, Smth = "ok", SeqArr = new int[] { 1, 2, 3 } });
            
            var msgSerializer = new MessageSerializer(new ProtobufSerializer(), new Microsoft.IO.RecyclableMemoryStreamManager());
            var ctSource = new CancellationTokenSource();
            var res = await msgSerializer.SerializeMessage(msg, ctSource.Token);
            Assert.IsTrue(res.Any());
            var msg2 = await msgSerializer.DeserializeMessage(res, ctSource.Token);
            Assert.AreEqual(msg.IsControl, msg2.IsControl);
            Assert.AreEqual(msg.PartitionKey, msg2.PartitionKey);

            if(msg.TryGetPayload<EventPayload>(out var payload1) && msg2.TryGetPayload<EventPayload>(out var payload2))
            {
                Assert.AreEqual(payload1.Event.Key, payload2.Event.Key);
            } else
            {
                Assert.Fail("could not retrieve event message payload");
            }

            if (msg.TryGetPayload<VectorClockMessagePayload>(out var clock1) && msg2.TryGetPayload<VectorClockMessagePayload>(out var clock2))
            {
                Assert.AreEqual(clock1.SeqNr, clock2.SeqNr);
                Assert.IsTrue(Enumerable.SequenceEqual(clock1.SeqArr, clock2.SeqArr));
            }
            else
            {
                Assert.Fail("could not retrieve clock message payload");
            }
            //Assert.AreEqual(metaData.Get("data"), msg2.PartitionKey);
        }

        [Test]
        public async Task DataMessageSerializes()
        {
            MessageBase msg = new DataMessage();
            msg.AddPayload(new EventPayload() { Event = new TestEvent() { Key = "k", Value = 1 } });
            msg.AddPayload(new VectorClockMessagePayload() { SeqNr = 111, Smth = "ok", SeqArr = new int[] { 1, 2, 3 } });

            var msgSerializer = new MessageSerializer(new ProtobufSerializer(), new Microsoft.IO.RecyclableMemoryStreamManager());
            var ctSource = new CancellationTokenSource();
            var res = await msgSerializer.SerializeMessage(msg, ctSource.Token);
            Assert.IsTrue(res.Any());
            var msg2 = await msgSerializer.DeserializeMessage(res, ctSource.Token);
            Assert.AreEqual(msg.IsControl, msg2.IsControl);
            Assert.AreEqual(msg.PartitionKey, msg2.PartitionKey);

            if (msg.TryGetPayload<EventPayload>(out var payload1) && msg2.TryGetPayload<EventPayload>(out var payload2))
            {
                Assert.AreEqual(payload1.Event.Key, payload2.Event.Key);
            }
            else
            {
                Assert.Fail("could not retrieve event message payload");
            }

            if (msg.TryGetPayload<VectorClockMessagePayload>(out var clock1) && msg2.TryGetPayload<VectorClockMessagePayload>(out var clock2))
            {
                Assert.AreEqual(clock1.SeqNr, clock2.SeqNr);
                Assert.IsTrue(Enumerable.SequenceEqual(clock1.SeqArr, clock2.SeqArr));
            }
            else
            {
                Assert.Fail("could not retrieve clock message payload");
            }
            //Assert.AreEqual(metaData.Get("data"), msg2.PartitionKey);
        }
    }


}
