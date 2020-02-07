using Apex.Serialization;
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
using Moq;
using System.Collections.Generic;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class BaseOutputEndpointTests
    {
        ICollection<IEvent> _testEvents;
        IOutputEndpoint _testEndpoint;
        CancellationTokenSource _ctSource;
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

            _testEvents = new List<IEvent>() {
                new TestEvent("test_key_0", 1337),
                new TestEvent("test_key_1", 1338),
                new TestEvent("test_key_2", 1339),
            };

            var serializerMock = new Mock<IEventSerializer>();
            serializerMock
                .Setup(s => s.SerializeEvent(It.IsAny<Stream>(), ref It.Ref<IEvent>.IsAny))
                .Callback<Stream, IEvent>((s, e) => s.Write(new byte[]{1}, 0, 1));
            //write a byte to the stream to mock serialized result 
            
            _testEndpoint = new TestOutputEndpoint(serializerMock.Object);
            _ctSource = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            foreach(var s in _streams)
            { s.Dispose(); }
        }

        [Test]
        public async Task EgressShouldWriteQueuedEventToStream()
        {   
            // one output stream + full enqueue
            var fakeShardId = 0;
            Assert.IsTrue(_testEndpoint.RegisterRemoteShard(fakeShardId), "Failed to register remote shard");
            var thread = Task.Run(() => _testEndpoint.Egress(_streams[0], fakeShardId, _ctSource.Token));
            _testEndpoint.EnqueueAll(_testEvents.ElementAt(0));
            await Task.Delay(10);
            _ctSource.Cancel();
            await thread;
            Assert.AreEqual(1, _streams[0].Length);
        }


        [Test]
        public async Task EgressShouldWriteEnqueueAllEventsToTwoStreams()
        {
            // two output streams + full enqueue
            var fakeShardIds = new int[] { 0, 1 };
            var threads = new Task[fakeShardIds.Length];
            foreach(var shardId in fakeShardIds)
            {
                Assert.IsTrue(_testEndpoint.RegisterRemoteShard(shardId), "Failed to register remote shard");
                threads[shardId] = Task.Run(() => _testEndpoint.Egress(_streams[shardId], shardId, _ctSource.Token));
            }
            _testEndpoint.EnqueueAll(_testEvents.ElementAt(0));
            _testEndpoint.EnqueueAll(_testEvents.ElementAt(1));
            await Task.Delay(10);
            _ctSource.Cancel();
            await Task.WhenAll(threads);
            
            Assert.AreEqual(2, _streams[0].Length);
            Assert.AreEqual(2, _streams[1].Length);
        }
    }
}