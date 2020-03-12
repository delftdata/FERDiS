using BlackSP.Core.Operators;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Operators;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Operator
{
    class BaseOperatorConfiguration : IOperatorConfiguration
    {}

    class TestBaseOperator : BaseOperator
    {
        public TestBaseOperator() : base(new BaseOperatorConfiguration())
        {}

        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            yield return @event; //base operator that just passes on events
        }
    }

    class NullBaseOperator : TestBaseOperator
    {
        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            return null;
        }
    }

    class ExceptionBaseOperator : TestBaseOperator
    {
        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            throw new NotImplementedException();
        }
    }

    public class BaseOperatorTests
    {
        private IOperator _operator;
        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _operator = new TestBaseOperator();

            _testEvents = new List<IEvent>();
            for (int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }

        }

        [Test]
        public async Task Enqueue_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => _operator.Enqueue(null));
        }

        [Test]
        public async Task Enqueue_ThrowsOnCancelled()
        {
            var operatorThread = _operator.Start();
            Assert.ThrowsAsync<OperationCanceledException>(_operator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operatorThread);
            Assert.Throws<OperationCanceledException>(() => _operator.Enqueue(new TestEvent()));
        }

        [Test]
        public async Task Stop_ThrowsWhenNotStarted()
        {
            //var operatorThread = _operator.Start();

            //TODO: update when custom exception implemented
            Assert.ThrowsAsync<Exception>(_operator.Stop);
        }

        [Test]
        public async Task Operator_ThrowsOnNullOperationResult()
        {
            _operator = new NullBaseOperator();
            var operatorThread = _operator.Start();
            _operator.Enqueue(new TestEvent());
            Assert.ThrowsAsync<NullReferenceException>(async () => await operatorThread);
            //also assert that after exception on operating thread the internal state is cancelled
            Assert.Throws<OperationCanceledException>(() => _operator.Enqueue(new TestEvent()));
        }

        [Test]
        public async Task Operator_ThrowsOnExceptionOperationResult()
        {
            _operator = new ExceptionBaseOperator();
            var operatorThread = _operator.Start();
            _operator.Enqueue(new TestEvent());
            Assert.ThrowsAsync<NotImplementedException>(async () => await operatorThread);
            
            //also assert that after an exception on the operating thread the operator has been cancelled and stopped processing
            Assert.Throws<OperationCanceledException>(() => _operator.Enqueue(new TestEvent()));
        }

        [Test]
        public async Task Operator_PassesAnEventThrough()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            var operatorThread = _operator.Start();

            _operator.Enqueue(_testEvents[0]);

            await Task.Delay(50); //give background thread some time to perform the operation
            Assert.ThrowsAsync<OperationCanceledException>(_operator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operatorThread);

            Assert.IsTrue(mockedOutputQueue.Any());
            Assert.AreEqual(_testEvents[0], mockedOutputQueue.Dequeue());
        }

        [Test]
        public async Task Operator_PassesEventsThroughInOrder()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            var operatorThread = _operator.Start();

            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
            }

            await Task.Delay(50); //give background thread some time to perform the operation
            Assert.ThrowsAsync<OperationCanceledException>(_operator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operatorThread);

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

            var operatorThread = _operator.Start();

            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
            }

            await Task.Delay(50); //give background thread some time to perform the operation

            Assert.ThrowsAsync<OperationCanceledException>(_operator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operatorThread);

            foreach (var outputQueue in outputQueues) //for every output enpoint..
            {
                Assert.IsTrue(outputQueue.Any());
                foreach (var e in _testEvents) //.. check that every event is in the queue, in order
                {
                    Assert.AreEqual(e, outputQueue.Dequeue());
                }
            }
        }
    }
}
