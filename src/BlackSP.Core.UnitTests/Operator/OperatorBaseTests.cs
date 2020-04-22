using BlackSP.Core.OperatorShells;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Operator
{
    class BaseOperator : IOperator
    {}

    class TestBaseOperatorShell : OperatorShellBase
    {
        public TestBaseOperatorShell() : base(new BaseOperator())
        {}

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            yield return @event; //base operator that just passes on events
        }
    }

    public class OperatorShellBaseTests
    {
        private OperatorShellBase _operator;
        private Task _operatorThread;
        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _operator = new TestBaseOperatorShell();

            _testEvents = new List<IEvent>();
            for (int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }

            _operatorThread = _operator.Start(DateTime.Now);

        }

        [Test]
        public void Enqueue_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => _operator.Enqueue(null));
        }

        [Test]
        public void Enqueue_ThrowsOnCancelled()
        {
            Assert.ThrowsAsync<OperationCanceledException>(_operator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await _operatorThread);
            Assert.Throws<OperationCanceledException>(() => _operator.Enqueue(new TestEvent()));
        }

        [Test]
        public void Stop_ThrowsWhenNotStarted()
        {
            var testoperator = new TestBaseOperatorShell();
            //TODO: update when custom exception implemented
            Assert.ThrowsAsync<Exception>(testoperator.Stop);
        }

        

        [Test]
        public async Task Operator_PassesAnEventThrough()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            _operator.Enqueue(_testEvents[0]);

            await Task.Delay(1); //give background thread some time to perform the operation

            Assert.IsTrue(mockedOutputQueue.Any());
            Assert.AreEqual(_testEvents[0], mockedOutputQueue.Dequeue());
        }

        [Test]
        public async Task Operator_PassesEventsThroughInOrder()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
            }

            await Task.Delay(1); //give background thread some time to perform the operation
            
            Assert.IsTrue(mockedOutputQueue.Any());
            foreach (var e in _testEvents)
            {
                Assert.AreEqual(e, mockedOutputQueue.Dequeue());
            }
        }

        [Test]
        public async Task Operator_PassesEventsThroughInOrder_AndToAllOutputEndpoints()
        {
            var outputQueues = new List<Queue<IEvent>>();
            //create three output queues
            for (int i = 0; i < 3; i++)
            {
                var mockedOutputQueue = new Queue<IEvent>();
                var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
                _operator.RegisterOutputEndpoint(outputEndpoint.Object);
                outputQueues.Add(mockedOutputQueue);
            }

            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
            }

            await Task.Delay(1); //give background thread some time to perform the operation

            foreach (var outputQueue in outputQueues) //for every output enpoint..
            {
                Assert.IsTrue(outputQueue.Any());
                foreach (var e in _testEvents) //.. check that every event is in the queue, in order
                {
                    Assert.AreEqual(e, outputQueue.Dequeue());
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            Assert.ThrowsAsync<OperationCanceledException>(_operator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await _operatorThread);

            _operator.Dispose();
        }
    }
}
