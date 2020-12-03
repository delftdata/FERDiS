using BlackSP.OperatorShells;
using BlackSP.Core.UnitTests.Events;
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

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    class EventCounterAggregateOperator : IAggregateOperator<TestEvent, TestEvent2>
    {
        public TimeSpan WindowSize { get; set; }
        public TimeSpan WindowSlideSize { get; set; }

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
            _windowSize = TimeSpan.FromMilliseconds(250);

            _operator = new AggregateOperatorShell<TestEvent, TestEvent2>(new EventCounterAggregateOperator
            {
                WindowSize = _windowSize,
                WindowSlideSize = _windowSize //tumbling window
            });
            _startTime = DateTime.Now;

            _testEvents = new List<TestEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent {
                    Key = $"K{i}", 
                    Value = (byte)i, 
                    EventTime = _startTime.AddMilliseconds(i) 
                });
            }
        }

        [Test]
        public async Task AggregateOperator_EmitsAResultFromWindow()
        {
            var results = _testEvents.SelectMany(e => _operator.OperateOnEvent(e)).ToArray();
            //insert extra event that is in the next window, closing the current window

            await Task.Delay(_windowSize);
            var windowCloser = new TestEvent
            {
                Key = "K_closer",
                Value = 10
            };
            var ok = _operator.OperateOnEvent(windowCloser);
            Assert.IsFalse(results.Any());
            Assert.IsTrue(ok.Any());
            var windowResult = ok.First() as TestEvent2;
            Assert.NotNull(windowResult);
            Assert.AreEqual(_testEvents.Count(), windowResult.Value);
        }

    }
}
