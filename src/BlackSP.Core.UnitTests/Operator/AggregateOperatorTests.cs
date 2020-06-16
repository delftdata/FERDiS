using BlackSP.Core.OperatorShells;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
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
    class EventCounterAggregateConfiguration : IAggregateOperator<TestEvent, TestEvent2>
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
        private DateTime _startTime;
        private AggregateOperatorShell<TestEvent, TestEvent2> _operator;
        private IList<TestEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _windowSize = TimeSpan.FromSeconds(5);
            _operator = new AggregateOperatorShell<TestEvent, TestEvent2>(new EventCounterAggregateConfiguration
            {
                WindowSize = _windowSize
            });
            _startTime = DateTime.Now;

            _testEvents = new List<TestEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i, EventTime = _startTime.AddMilliseconds(i) });
            }
        }

        [Test]
        public async Task AggregateOperator_EmitsAResultFromWindow()
        {
            var results = _testEvents.SelectMany(e => _operator.OperateOnEvent(e)).ToArray();
            //insert extra event that is in the next window, thus closing the current window
            var windowCloser = new TestEvent
            {
                Key = "K_closer",
                EventTime = _startTime + _windowSize,
                Value = 10
            };
            var ok = _operator.OperateOnEvent(windowCloser);
            Assert.IsFalse(results.Any());
            Assert.IsTrue(ok.Any());
            var windowResult = ok.First() as TestEvent2;
            Assert.NotNull(windowResult);
            Assert.AreEqual(_testEvents.Count(), windowResult.Value);
        }
        /*
        [Test]
        public async Task AggregateOperator_EmitsMultipleResults()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _operator.RegisterOutputEndpoint(outputEndpoint.Object);

            var testStartTimes = new DateTime[] { DateTime.Now, DateTime.Now + _windowSize, DateTime.Now + (3 * _windowSize) };
            foreach (var startTime in testStartTimes)
            {
                var testEvents = new List<TestEvent>();
                for (int i = 0; i < 10; i++)
                {
                    testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i, EventTime = startTime });
                }

                foreach (var e in testEvents)
                {
                    _operator.Enqueue(e); //enqueue events in window
                }
            }

            var windowCloser = new TestEvent
            {
                Key = "K_closer",
                EventTime = testStartTimes.Last().Add(_windowSize),
                Value = 10
            };
            _operator.Enqueue(windowCloser); //enqueue events in window
            
            await Task.Delay(100);

            lock (mockedOutputQueue)
            {
                for (int i = 1; i <= 3; i++)
                {   //expect three window results (one empty in the middle does not get emitted)
                    Assert.IsTrue(mockedOutputQueue.Any());
                    var windowResult = mockedOutputQueue.Dequeue() as TestEvent2;
                    Assert.NotNull(windowResult);
                    var expectedEvents = _testEvents.Count();
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
        }*/
    }
}
