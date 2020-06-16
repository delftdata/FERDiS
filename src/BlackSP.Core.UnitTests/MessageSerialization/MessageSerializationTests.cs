using BlackSP.Core.Models;
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
    [ProtoContract, ProtoInclude(10, typeof(MessageA))]
    public abstract class MessageBase : IMessage
    {
        [ProtoMember(1)]
        public virtual bool IsControl { get; set; }

        [ProtoMember(2)]
        public virtual int PartitionKey { get; set; }
    }

    public class MessageA : MessageBase
    {
        [ProtoMember(3)]
        public int PropertyA { get; set; }
    }

    public class MessageSerializationTests
    {
        [Test]
        public async Task MessageSerializes()
        {
            var msg = new MessageA
            {
                IsControl = true,
                PartitionKey = 42
            };
            var msgSerializer = new MessageSerializer(new ProtobufSerializer(), new Microsoft.IO.RecyclableMemoryStreamManager());
            var ctSource = new CancellationTokenSource();
            var res = await msgSerializer.SerializeMessage(msg, ctSource.Token);
            Assert.IsTrue(res.Any());
            var msg2 = await msgSerializer.DeserializeMessage(res, ctSource.Token);
            Assert.AreEqual(msg.IsControl, msg2.IsControl);
            Assert.AreEqual(msg.PartitionKey, msg2.PartitionKey);
        }
    }


}
