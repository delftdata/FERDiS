using BlackSP.Core.OperatorShells;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Operator
{
    class JoinOperatorConfigurationForTest : IJoinOperator<TestEvent, TestEvent2, TestEvent2>
    {
        public TimeSpan WindowSize { get; set; }

        public TestEvent2 Join(TestEvent matchA, TestEvent2 matchB)
        {
            return new TestEvent2
            {
                Key = $"{matchA.Key}+{matchB.Key}",
                EventTime = matchA.EventTime,
                Value = matchA.Value + matchB.Value
            };
        }

        public bool Match(TestEvent testA, TestEvent2 testB)
        {
            return testA.Value == testB.Value;
        }
    }

    public class JoinOperatorTests
    {

        private JoinOperatorShell<TestEvent, TestEvent2, TestEvent2> _operator;
        private TimeSpan _windowSize;
        private DateTime _startTime;
        private Task _operatorThread;

        [SetUp]
        public void SetUp()
        {
            _startTime = DateTime.Now;
            _windowSize = TimeSpan.FromSeconds(10);
            _operator = new JoinOperatorShell<TestEvent, TestEvent2, TestEvent2>(new JoinOperatorConfigurationForTest { WindowSize = _windowSize });
            _operatorThread = _operator.Start(_startTime);
        }
        
        [Test]
        public async Task FilterOperator_JoinsTwoMatchingEventsInWindow()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            IList<TestEvent> events = new List<TestEvent>();
            IList<TestEvent2> events2 = new List<TestEvent2>();

            events.Add(new TestEvent { Key = "KA", EventTime = _startTime.AddSeconds(1), Value = 1 });
            events.Add(new TestEvent { Key = "KA2", EventTime = _startTime.AddSeconds(1), Value = 0 });
            events2.Add(new TestEvent2 { Key = "KB", EventTime = _startTime.AddSeconds(1), Value = 1 });

            foreach(var @event in events.AsEnumerable<IEvent>().Concat(events2))
            {
                _operator.Enqueue(@event);
            }

            await Task.Delay(10);

            Assert.IsNotEmpty(mockedOutputQueue);
            var outputEvent = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(2, outputEvent.Value);
            Assert.AreEqual("KA+KB", outputEvent.Key);
            Assert.IsEmpty(mockedOutputQueue);
        }

        [Test]
        public async Task FilterOperator_JoinsAllMatchingEventsInWindow()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            IList<IEvent> events = new List<IEvent>();

            events.Add(new TestEvent { Key = "K1_A", EventTime = _startTime.AddSeconds(1), Value = 1 });
            events.Add(new TestEvent2 { Key = "K2_A", EventTime = _startTime.AddSeconds(2), Value = 1 });
            events.Add(new TestEvent { Key = "K1_B", EventTime = _startTime.AddSeconds(3), Value = 0 });
            events.Add(new TestEvent2 { Key = "K2_B", EventTime = _startTime.AddSeconds(4), Value = 2 });
            events.Add(new TestEvent2 { Key = "K2_C", EventTime = _startTime.AddSeconds(5), Value = 1 });
            //matches are on value so (K1_A, K2_A) and (K2_C, K1_A)
            foreach (var @event in events)
            {
                _operator.Enqueue(@event);
            }
            
            await Task.Delay(25);

            Assert.IsNotEmpty(mockedOutputQueue);
            var outputEvent = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(2, outputEvent.Value);
            Assert.AreEqual("K1_A+K2_A", outputEvent.Key);
            
            Assert.IsNotEmpty(mockedOutputQueue);
            outputEvent = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(2, outputEvent.Value);
            Assert.AreEqual("K1_A+K2_C", outputEvent.Key);
            Assert.IsEmpty(mockedOutputQueue);

        }

        [Test]
        public async Task FilterOperator_ShouldNotJoinWithEventsOutOfWindow()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            IList<IEvent> events = new List<IEvent>();

            events.Add(new TestEvent { Key = "K1_A", EventTime = _startTime.AddSeconds(1), Value = 2 });
            events.Add(new TestEvent2 { Key = "K2_A", EventTime = _startTime.Add(_windowSize).AddSeconds(1), Value = 2 });
            events.Add(new TestEvent { Key = "K1_B", EventTime = _startTime.Add(_windowSize).AddSeconds(2), Value = 2 });
            //second two events cause first event to go out of window so shouldnt return as a match
            foreach (var @event in events)
            {
                _operator.Enqueue(@event);
            }

            await Task.Delay(25);

            Assert.IsNotEmpty(mockedOutputQueue);
            var outputEvent = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(4, outputEvent.Value);
            Assert.AreEqual("K1_B+K2_A", outputEvent.Key);
            
            Assert.IsEmpty(mockedOutputQueue);
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
