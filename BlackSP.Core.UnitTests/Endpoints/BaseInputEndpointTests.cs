using BlackSP.Core.Events;
using BlackSP.Core.Endpoints;
using BlackSP.Core.UnitTests.Events;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using BlackSP.Core.Serialization;
using Apex.Serialization;
using System.Collections.Generic;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class BaseInputEndpointTests
    {
        ApexEventSerializer _serializer;
        ICollection<IEvent> _testEvents;
        IInputEndpoint _testEndpoint;
        CancellationTokenSource _ctSource;

        [SetUp]
        public void Setup()
        {
            //TODO moq serializer
            _serializer = new ApexEventSerializer(Binary.Create());

            _testEvents = new List<IEvent>() {
                new TestEvent("test_key_0", 1337),
                new TestEvent("test_key_1", 1338),
                new TestEvent("test_key_2", 1339),
            };
            _testEndpoint = new TestInputEndpoint();

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
                    _serializer.SerializeEvent(msgBuffer, @event);
                }

                //Set position back to start of stream to be able to read the written messages
                msgBuffer.Seek(0, SeekOrigin.Begin);

                //start processing from stream
                var inputThread = Task.Run(() => _testEndpoint.Ingress(msgBuffer, _ctSource.Token));

                //a bit hackish but we need to wait for the background thread to do its work
                await Task.Delay(250);

                //cancel reading thread and wait for background thread to exit
                _ctSource.Cancel(); 
                await inputThread;
            }

            //assertions
            foreach (var @event in _testEvents)
            {
                Assert.IsTrue(_testEndpoint.HasInput(), "No Input");
                var resultEvent = (TestEvent)_testEndpoint.GetNext();
                Assert.IsNotNull(resultEvent, "Event is null");
                Assert.AreEqual(resultEvent.GetValue(), @event.GetValue(), "Unequal values");
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
                await Task.Delay(250);

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