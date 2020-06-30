using BlackSP.OperatorShells;
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
        public void AggregateOperator_EmitsAResultFromWindow()
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

    }
}
