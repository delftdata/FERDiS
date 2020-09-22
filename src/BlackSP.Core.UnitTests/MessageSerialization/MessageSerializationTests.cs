using BlackSP.Core.Models;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Models;
using BlackSP.Serialization;
using Microsoft.IO;
using NUnit.Framework;
using ProtoBuf;
using System.Linq;
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

    [ProtoContract]
    public class TestPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "data:test";

        [ProtoMember(1)]
        public IEvent Event { get; set; }

    }

    public class MessageSerializationTests
    {
        [Test]
        public async Task ControlMessageSerializes()
        {
            MessageBase msg = new ControlMessage();
            msg.AddPayload(new TestPayload() { Event = new TestEvent() { Key = "k", Value = 1 } });
            msg.AddPayload(new VectorClockMessagePayload() { SeqNr = 111, Smth = "ok", SeqArr = new int[] { 1, 2, 3 } });
            
            var msgSerializer = new PooledBufferMessageSerializer(new ProtobufStreamSerializer(), new RecyclableMemoryStreamManager());
            var ctSource = new CancellationTokenSource();
            var res = await msgSerializer.SerializeAsync(msg, ctSource.Token);
            Assert.IsTrue(res.Any());
            var msg2 = await msgSerializer.DeserializeAsync<ControlMessage>(res, ctSource.Token);
            Assert.AreEqual(msg.IsControl, msg2.IsControl);
            Assert.AreEqual(msg.PartitionKey, msg2.PartitionKey);

            if(msg.TryExtractPayload<TestPayload>(out var payload1) && msg2.TryExtractPayload<TestPayload>(out var payload2))
            {
                Assert.AreEqual(payload1.Event.Key, payload2.Event.Key);
            } else
            {
                Assert.Fail("could not retrieve event message payload");
            }

            if (msg.TryExtractPayload<VectorClockMessagePayload>(out var clock1) && msg2.TryExtractPayload<VectorClockMessagePayload>(out var clock2))
            {
                Assert.AreEqual(clock1.SeqNr, clock2.SeqNr);
                Assert.IsTrue(Enumerable.SequenceEqual(clock1.SeqArr, clock2.SeqArr));
            }
            else
            {
                Assert.Fail("could not retrieve clock message payload");
            }
            Assert.IsEmpty(msg.Payloads);
            Assert.IsEmpty(msg2.Payloads);
        }

        [Test]
        public async Task DataMessageSerializes()
        {
            MessageBase msg = new DataMessage();
            msg.AddPayload(new TestPayload() { Event = new TestEvent() { Key = "k", Value = 1 } });
            msg.AddPayload(new VectorClockMessagePayload() { SeqNr = 111, Smth = "ok", SeqArr = new int[] { 1, 2, 3 } });

            var msgSerializer = new PooledBufferMessageSerializer(new ProtobufStreamSerializer(), new RecyclableMemoryStreamManager());
            var ctSource = new CancellationTokenSource();
            var res = await msgSerializer.SerializeAsync(msg, ctSource.Token);
            Assert.IsTrue(res.Any());
            var msg2 = await msgSerializer.DeserializeAsync<DataMessage>(res, ctSource.Token);
            Assert.AreEqual(msg.IsControl, msg2.IsControl);
            Assert.AreEqual(msg.PartitionKey, msg2.PartitionKey);

            if (msg.TryExtractPayload<TestPayload>(out var payload1) && msg2.TryExtractPayload<TestPayload>(out var payload2))
            {
                Assert.AreEqual(payload1.Event.Key, payload2.Event.Key);
            }
            else
            {
                Assert.Fail("could not retrieve event message payload");
            }

            if (msg.TryExtractPayload<VectorClockMessagePayload>(out var clock1) && msg2.TryExtractPayload<VectorClockMessagePayload>(out var clock2))
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
