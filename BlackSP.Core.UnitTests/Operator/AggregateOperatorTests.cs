using BlackSP.Core.Operators;
using BlackSP.Core.Operators.Concrete;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Interfaces.Endpoints;
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
    class EventCounterAggregateConfiguration : IAggregateOperatorConfiguration<TestEvent, TestEvent2>
    {
        public TimeSpan WindowSize { get; set; }

        public IEnumerable<TestEvent2> Aggregate(IEnumerable<TestEvent> window)
        {
            yield return new TestEvent2
            {
                Key = "AggregateResult",
                Value = window.Count()
            };
        }
    }

    public class AggregateOperatorTests
    {
        private TimeSpan _windowSize;
        private Task _operatorThread;
        private AggregateOperator<TestEvent, TestEvent2> _operator;
        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _windowSize = TimeSpan.FromMilliseconds(50);
            _operator = new AggregateOperator<TestEvent, TestEvent2>(new EventCounterAggregateConfiguration
            {
                WindowSize = _windowSize
            });
            _testEvents = new List<IEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }
            _operatorThread = _operator.Start();
        }

        [Test]
        public async Task AggregateOperator_EmitsAResultFromWindow()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            await Task.Delay(1); //slightly skew of window barriers
            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
            }

            await Task.Delay(_windowSize); //let the window close
            
            
            Assert.IsTrue(mockedOutputQueue.Any());

            var windowResult = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.NotNull(windowResult);
            Assert.AreEqual(_testEvents.Count(), windowResult.Value);

            Assert.IsFalse(mockedOutputQueue.Any());
        }

        [Test]
        public async Task AggregateOperator_EmitsMultipleResults()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            await Task.Delay(_windowSize / 10); //slightly skew off operator window barriers
            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
            }
            
            await Task.Delay(_windowSize); //let the window close

            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e);
                _operator.Enqueue(e); //second window put double events
            }
            await Task.Delay(_windowSize); //let the window close

            await Task.Delay(_windowSize); //leave third window empty

            foreach (var e in _testEvents)
            {
                _operator.Enqueue(e); _operator.Enqueue(e);
                _operator.Enqueue(e); _operator.Enqueue(e); //fourth window put quadruple events
            }
            await Task.Delay(_windowSize); //let the window close

            lock(mockedOutputQueue)
            {
                for (int i = 1; i <= 4; i++)
                {   //two iterations for the two expected windows
                    Assert.IsTrue(mockedOutputQueue.Any());
                    var windowResult = mockedOutputQueue.Dequeue() as TestEvent2;
                    Assert.NotNull(windowResult);
                    var expectedEvents = i == 3 ? 0 : _testEvents.Count() * i; //third window is empty in test
                    Assert.AreEqual(expectedEvents, windowResult.Value);
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
