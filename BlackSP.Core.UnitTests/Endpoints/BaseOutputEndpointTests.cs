using BlackSP.Core.Endpoints;
using BlackSP.Core.UnitTests.Events;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using Moq;
using System.Collections.Generic;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class BaseOutputEndpointTests
    {
        ICollection<IEvent> _testEvents;
        IOutputEndpoint _testEndpoint;
        ISerializer _serializer;
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
                new TestEvent("test_key_0", 0),
                new TestEvent("test_key_1", 1),
                new TestEvent("test_key_2", 2),
            };

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

            _testEndpoint = new TestOutputEndpoint(serializerMoq.Object);
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
            try
            {
                await Task.Delay(10);
                _ctSource.Cancel();
                await thread;
            } catch(OperationCanceledException e) {};

            _streams[0].Seek(0, SeekOrigin.Begin);
            Assert.AreEqual(1, _streams[0].Length);
            Assert.AreEqual(_testEvents.ElementAt(0), _serializer.Deserialize<IEvent>(_streams[0], _ctSource.Token));
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
            
            try
            {
                await Task.Delay(100);
                _ctSource.Cancel();
                await Task.WhenAll(threads);
            }
            catch (OperationCanceledException e) { };

            _streams[0].Seek(0, SeekOrigin.Begin);
            _streams[1].Seek(0, SeekOrigin.Begin);

            Assert.AreEqual(2, _streams[0].Length);
            Assert.AreEqual(_testEvents.ElementAt(0), _serializer.Deserialize<IEvent>(_streams[0], _ctSource.Token));
            Assert.AreEqual(_testEvents.ElementAt(1), _serializer.Deserialize<IEvent>(_streams[0], _ctSource.Token));


            Assert.AreEqual(2, _streams[1].Length);
            Assert.AreEqual(_testEvents.ElementAt(0), _serializer.Deserialize<IEvent>(_streams[1], _ctSource.Token));
            Assert.AreEqual(_testEvents.ElementAt(1), _serializer.Deserialize<IEvent>(_streams[1], _ctSource.Token));

        }
    }
}