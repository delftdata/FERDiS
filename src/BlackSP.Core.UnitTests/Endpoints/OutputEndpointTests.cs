using BlackSP.Core.Endpoints;
using BlackSP.Core.Extensions;
using BlackSP.Core.UnitTests.Events;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using Moq;
using System.Collections.Generic;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Kernel.Operators;
using Microsoft.IO;
using BlackSP.Core.UnitTests.Utilities;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class OutputEndpointTests
    {
        ICollection<IEvent> _testEvents;
        OutputEndpoint _testEndpoint;
        IStreamSerializer _serializer;
        CancellationTokenSource _endpointCtSource;
        CancellationTokenSource _operatorCtSource;
        Stream[] _streams;
        private int _streamCount;

        //test cases to implement: 
        // - one output stream + partition enqueue
        // - two output streams + partition enqueue
        // - two output streams on same shard id --> should error

        // show correctness by re-serializing the streams and checking if all came back in the same order


        [SetUp]
        public void Setup()
        {
            _streamCount = 5;
            _streams = new Stream[_streamCount];
            for (int i = 0; i < _streamCount; i++)
            {
                _streams[i] = new MemoryStream();
            }
            _endpointCtSource = new CancellationTokenSource();
            _operatorCtSource = new CancellationTokenSource();

            _testEvents = new List<IEvent>() {
                new TestEvent{ Key = "test_key_0", Value = 0 },
                new TestEvent{ Key = "test_key_1", Value = 1 },
                new TestEvent{ Key = "test_key_2", Value = 2 },
            };

            var serializerMoq = MockBuilder.MockSerializer(_testEvents);
            _serializer = serializerMoq.Object;

            var operatorMoq = new Mock<IOperatorShell>();
            //operatorMoq.Setup(o => o.CancellationToken).Returns(() => _operatorCtSource.Token);

            var recycleMemStreamManager = new RecyclableMemoryStreamManager();

            //_testEndpoint = new OutputEndpoint(operatorMoq.Object, _serializer, recycleMemStreamManager);
        }

        [Test]
        public async Task EgressShouldWriteQueuedEventToStream()
        {
            // one output stream + full enqueue
            var fakeShardId = 0;
            //Assert.IsTrue(_testEndpoint.RegisterRemoteShard(fakeShardId), "Failed to register remote shard");

            //var thread = Task.Run(async () => await _testEndpoint.Egress(_streams[0], fakeShardId, _endpointCtSource.Token));
            //_testEndpoint.Enqueue(_testEvents.ElementAt(0), OutputMode.Broadcast);
            
            await Task.Delay(50); //wait for threads to do work

            _streams[0].Seek(0, SeekOrigin.Begin);

            //assertions
            Assert.AreEqual(2, _streams[0].Length);
            //await _streams[0].ReadInt32Async(); //strip leading int
            var nextEventFromStream = await _serializer.Deserialize<IEvent>(_streams[0], _endpointCtSource.Token);
            Assert.AreEqual(_testEvents.ElementAt(0), nextEventFromStream);

            //teardown
            try
            {
                _endpointCtSource.Cancel();
                //await thread;
            }
            catch (OperationCanceledException) { };
        }


        [Test]
        public async Task EgressShouldWriteEnqueueAllEventsToTwoStreams()
        {
            // two output streams + full enqueue
            var fakeShardIds = new int[] { 0, 1 };
            var threads = new Task[fakeShardIds.Length];
            
            foreach (var shardId in fakeShardIds)
            {
                //Assert.IsTrue(_testEndpoint.RegisterRemoteShard(shardId), "Failed to register remote shard");
                //threads[shardId] = Task.Run(async () => await _testEndpoint.Egress(_streams[shardId], shardId, _endpointCtSource.Token));
            }
            //_testEndpoint.Enqueue(_testEvents, OutputMode.Broadcast);
            //_testEndpoint.Enqueue(_testEvents.ElementAt(1), OutputMode.Broadcast);

            await Task.Delay(50); //allow threads to work

            foreach(var shardId in fakeShardIds)
            {
                _streams[shardId].Seek(0, SeekOrigin.Begin); //to allow reading the streams
                //do assertions
                Assert.AreEqual(2 * _testEvents.Count, _streams[shardId].Length); //1 byte for leading int + 1 byte for event (only works like this in test)
                
                foreach(var @event in _testEvents)
                {
                    //await _streams[shardId].ReadInt32Async(); //strip leading int
                    var nextEvent = await _serializer.Deserialize<IEvent>(_streams[shardId], _endpointCtSource.Token);
                    Assert.AreEqual(@event.Key, nextEvent.Key, $"Mismatch on 1st event for shard {shardId}");
                }
            }

            //teardown
            try
            {
                _endpointCtSource.Cancel();
                await Task.WhenAll(threads);
            }
            catch (OperationCanceledException) { };
        }

        [Test]
        public void Enqueue_ShouldThrowOnNull()
        {
            IEvent nullEvent = null;
            //Assert.Throws<ArgumentNullException>(() => _testEndpoint.Enqueue(nullEvent, OutputMode.Broadcast));
        }

        [Test]
        public void Enqueue_Overload_ShouldThrowOnNull()
        {
            IEnumerable<IEvent> nullEnumerable = null;
            //Assert.Throws<ArgumentNullException>(() => _testEndpoint.Enqueue(nullEnumerable, OutputMode.Broadcast));
        }

        [Test]
        public void RegisterWithSameShardIdShouldReturnFalse()
        {
            //Assert.IsTrue(_testEndpoint.RegisterRemoteShard(0));
            //Assert.IsTrue(_testEndpoint.RegisterRemoteShard(1));
            //Assert.IsFalse(_testEndpoint.RegisterRemoteShard(0));
        }

        [TearDown]
        public async Task TearDown()
        {
            _operatorCtSource.Cancel();
            _endpointCtSource.Cancel();
            await Task.Delay(10);
            //_testEndpoint.Dispose();

            foreach (var s in _streams)
            { s.Dispose(); }
        }
    }
}