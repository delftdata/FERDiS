using BlackSP.Kernel.Events;
using BlackSP.Kernel.Serialization;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using BlackSP.Core.Streams;
using BlackSP.Core.UnitTests.Events;
using System.Threading.Tasks;
using System.Linq;
using BlackSP.Kernel.Operators;
using System.Collections.Concurrent;
using BlackSP.Kernel.Endpoints;

namespace BlackSP.Core.UnitTests.Utilities
{
    public static class MockBuilder
    {
        public static Mock<IOperatorSocket> MockOperator(CancellationTokenSource operatorCtSource, BlockingCollection<IEvent> inputQueue)
        {
            var operatorMoq = new Mock<IOperatorSocket>();
            operatorMoq.Setup(o => o.CancellationToken).Returns(() => operatorCtSource.Token);
            operatorMoq.Setup(o => o.Enqueue(It.IsAny<IEvent>()))
                .Callback((IEvent e) => inputQueue.Add(e));
            return operatorMoq;
        }

        public static Mock<ISerializer> MockSerializer(ICollection<IEvent> testEvents)
        {
            var serializerMoq = new Mock<ISerializer>();
            serializerMoq
                .Setup(ser => ser.Serialize(It.IsAny<Stream>(), It.IsAny<IEvent>()))
                .Callback<Stream, IEvent>((s, e) =>
                {
                    s.Write(new byte[] { ((TestEvent)e).Value }, 0, 1);
                });
            serializerMoq
                .Setup(ser => ser.Deserialize<IEvent>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, e) => {
                    int c = s.ReadByte();
                    return Task.FromResult(testEvents.FirstOrDefault(ev => c == ((TestEvent)ev).Value));
                });

            return serializerMoq;
        }

        public static Mock<IOutputEndpoint> MockOutputEndpoint(Queue<IEvent> targetQueue)
        {
            var outputEndpoint = new Mock<IOutputEndpoint>();
            outputEndpoint.Setup(x => x.Enqueue(It.IsAny<IEvent>(), It.IsAny<OutputMode>()))
                .Callback((IEvent e, OutputMode m) => targetQueue.Enqueue(e));
            outputEndpoint.Setup(x => x.Enqueue(It.IsAny<IEnumerable<IEvent>>(), It.IsAny<OutputMode>()))
                .Callback((IEnumerable<IEvent> es, OutputMode m) =>
                {
                    foreach (var e in es)
                    {
                        targetQueue.Enqueue(e);
                    }
                });
            return outputEndpoint;
        }
    }
}
