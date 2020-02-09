using BlackSP.Core.UnitTests.Events;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;
using Moq;
using System.Linq;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class BaseInputEndpointTests
    {
        ISerializer _serializer;
        IList<IEvent> _testEvents;
        IInputEndpoint _testEndpoint;
        CancellationTokenSource _ctSource;

        [SetUp]
        public void Setup()
        {
            var serializerMoq = new Mock<ISerializer>();
            serializerMoq
                .Setup(ser => ser.Serialize(It.IsAny<Stream>(), It.IsAny<IEvent>()))
                .Callback<Stream, IEvent>((s, e) => s.Write(new byte[] { (byte)e.GetValue() }, 0, 1));
            serializerMoq
                .Setup(ser => ser.Deserialize<IEvent>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((s, e) => {
                    int c = s.ReadByte();
                    return _testEvents.FirstOrDefault(ev => c == (byte)ev.GetValue());
                });
            _serializer = serializerMoq.Object;

            _testEvents = new List<IEvent>() {
                new TestEvent("test_key_0", (byte)0),
                new TestEvent("test_key_1", (byte)1),
                new TestEvent("test_key_2", (byte)2),
            };
            _testEndpoint = new TestInputEndpoint(_serializer);

            _ctSource = new CancellationTokenSource();
        }

        [Test]
        public async Task Ingress_Should_ReturnEventsFromStream()
        {
            using (Stream msgBuffer = new MemoryStream())
            {
                //write test event to stream
                var eventEnumerator = _testEvents.GetEnumerator();
                while(eventEnumerator.MoveNext())
                {
                    IEvent @event = eventEnumerator.Current;
                    await _serializer.Serialize(msgBuffer, @event);
                }

                //Set position back to start of stream to be able to read the written messages
                msgBuffer.Seek(0, SeekOrigin.Begin);

                //start processing from stream
                var inputThread = Task.Run(() => _testEndpoint.Ingress(msgBuffer, _ctSource.Token));

                //a bit hackish but we need to wait for the background thread to do its work
                await Task.Delay(10);

                //cancel reading thread and wait for background thread to exit
                _ctSource.Cancel(); 
                await inputThread;

                //assertions
                foreach (var @event in _testEvents)
                {
                    Assert.IsTrue(_testEndpoint.HasInput(), "No Input");
                    var resultEvent = (TestEvent)_testEndpoint.GetNext();
                    Assert.IsNotNull(resultEvent, "Event is null");
                    Assert.AreEqual(resultEvent.GetValue(), @event.GetValue(), "Unequal values");
                }
            }
        }

        [Test]
        public async Task HasInput_Should_ReturnFalse_WhenNoInput()
        {
            using (Stream inputStream = new MemoryStream())
            {
                //start processing from stream
                var inputThread = Task.Run(() => _testEndpoint.Ingress(inputStream, _ctSource.Token));

                //Let the background thread operate for a bit..
                await Task.Delay(100);

                //cancel reading thread
                _ctSource.Cancel();
                await inputThread;
            }
            //assertions
            Assert.IsFalse(_testEndpoint.HasInput());
            Assert.IsNull(_testEndpoint.GetNext());
        }

    }
}