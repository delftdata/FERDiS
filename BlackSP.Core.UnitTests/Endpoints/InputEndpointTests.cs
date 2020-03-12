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
using BlackSP.Core.Endpoints;
using BlackSP.Core.Streams;
using System;
using BlackSP.Core.UnitTests.Utilities;
using System.Buffers;
using BlackSP.Interfaces.Operators;
using System.Collections.Concurrent;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class InputEndpointTests
    {
        IOperator _targetOperator;
        BlockingCollection<IEvent> _targetOperatorInputqueue;

        ISerializer _serializer;
        IList<IEvent> _testEvents;
        IInputEndpoint _testEndpoint;
        CancellationTokenSource _endpointCtSource;
        CancellationTokenSource _operatorCtSource;
        
        [SetUp]
        public void Setup()
        {
            _endpointCtSource = new CancellationTokenSource();
            _operatorCtSource = new CancellationTokenSource();

            _testEvents = new List<IEvent>() {
                new TestEvent{ Key = "test_key_0", Value = 0 },
                new TestEvent{ Key = "test_key_1", Value = 1 },
                new TestEvent{ Key = "test_key_2", Value = 2 },
            };

            var serializerMoq = MockBuilder.MockSerializer(_testEvents);
            _serializer = serializerMoq.Object;

            _targetOperatorInputqueue = new BlockingCollection<IEvent>();
            var operatorMoq = MockBuilder.MockOperator(_operatorCtSource, _targetOperatorInputqueue);
            _targetOperator = operatorMoq.Object;

            var arrayPool = ArrayPool<byte>.Create();
            _testEndpoint = new InputEndpoint(_targetOperator, _serializer, arrayPool);
        }

        [Test]
        public async Task Ingress_Should_ReturnEventsFromStream()
        {
            using (Stream testIngressStream = new MemoryStream())
            {
                //write test event to stream
                var eventEnumerator = _testEvents.GetEnumerator();
                while(eventEnumerator.MoveNext())
                {
                    using(Stream tempBuffer = new MemoryStream())
                    {
                        IEvent @event = eventEnumerator.Current;
                        await _serializer.Serialize(tempBuffer, @event);
                        testIngressStream.WriteInt32((int)tempBuffer.Length);
                        tempBuffer.Seek(0, SeekOrigin.Begin);
                        tempBuffer.CopyTo(testIngressStream);
                    }
                    
                }
                //Set position back to start of stream to be able to read the written messages
                testIngressStream.Seek(0, SeekOrigin.Begin);
                //start processing from stream
                var inputThread = Task.Run(() => _testEndpoint.Ingress(testIngressStream, _endpointCtSource.Token));
                //a bit hackish but we need to wait for the background thread to do its work
                await Task.Delay(100);
                
                //assertions
                foreach (var @event in _testEvents)
                {
                    Assert.IsTrue(_targetOperatorInputqueue.Any(), "Empty input queue");
                    var resultEvent = _targetOperatorInputqueue.Take();
                    Assert.IsNotNull(resultEvent, "Event is null");
                    Assert.AreEqual(@event.Key, resultEvent.Key);
                    
                    Assert.AreEqual(((TestEvent)resultEvent).Value, ((TestEvent)@event).Value, "Unequal values");
                }

                //teardown
                //cancel reading thread and wait for background thread to exit
                _endpointCtSource.Cancel();
                await inputThread;
            }
        }

        [Test]
        public async Task HasInput_Should_ReturnFalse_WhenNoInput()
        {
            using (Stream inputStream = new MemoryStream())
            {
                //start processing from stream
                var inputThread = Task.Run(() => _testEndpoint.Ingress(inputStream, _endpointCtSource.Token));
                //Let the background thread operate for a bit..
                await Task.Delay(100);
                //cancel reading thread

                //teardown
                _endpointCtSource.Cancel();
                await inputThread;
            }
            //assertions
            Assert.IsFalse(_targetOperatorInputqueue.Any());            
        }

    }
}