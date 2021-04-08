using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    class JoinOperatorConfigurationForTest : IJoinOperator<TestEvent, TestEvent2, TestEvent2>
    {
        public TimeSpan WindowSize { get; set; }
        public TimeSpan WindowSlideSize { get; set; }

        public TestEvent2 Join(TestEvent matchA, TestEvent2 matchB)
        {
            return new TestEvent2
            {
                Key = matchA.Key+matchB.Key,
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
        private TimeSpan _windowSlideSize;
        private DateTime _startTime;

        [SetUp]
        public void SetUp()
        {
            _startTime = DateTime.Now;
            _windowSize = TimeSpan.FromMilliseconds(500);
            _windowSlideSize = TimeSpan.FromMilliseconds(500);
            _operator = new JoinOperatorShell<TestEvent, TestEvent2, TestEvent2>(new JoinOperatorConfigurationForTest { 
                WindowSize = _windowSize,
                WindowSlideSize = _windowSlideSize
            });
        }
        
        [Test]
        public async Task JoinOperator_JoinsTwoMatchingEventsInWindow()
        {
            var output = new List<IEvent>();

            IList<TestEvent> events = new List<TestEvent>();
            IList<TestEvent2> events2 = new List<TestEvent2>();

            events.Add(new TestEvent { Key = 1, EventTime = _startTime.AddSeconds(1), Value = 1 });
            events.Add(new TestEvent { Key = 10, EventTime = _startTime.AddSeconds(1), Value = 0 });
            events2.Add(new TestEvent2 { Key = 100, EventTime = _startTime.AddSeconds(1), Value = 1 });

            foreach(var @event in events.AsEnumerable<IEvent>().Concat(events2))
            {
                output.AddRange(await _operator.OperateOnEvent(@event));
            }

            Assert.IsNotEmpty(output);
            var outputEvent = output.First() as TestEvent2;
            output.RemoveAt(0);
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(2, outputEvent.Value);
            Assert.AreEqual(101, outputEvent.Key);
            Assert.IsEmpty(output);
        }

        [Test]
        public async Task JoinOperator_JoinsAllMatchingEventsInWindow()
        {
            var output = new List<IEvent>();
            IList<IEvent> events = new List<IEvent>();

            events.Add(new TestEvent { Key = 1, EventTime = _startTime.AddSeconds(1), Value = 1 });
            events.Add(new TestEvent2 { Key = 10, EventTime = _startTime.AddSeconds(2), Value = 1 });
            events.Add(new TestEvent { Key = 100, EventTime = _startTime.AddSeconds(3), Value = 0 });
            events.Add(new TestEvent2 { Key = 1000, EventTime = _startTime.AddSeconds(4), Value = 2 });
            events.Add(new TestEvent2 { Key = 10000, EventTime = _startTime.AddSeconds(5), Value = 1 });
            //matches are on value so (K1_A, K2_A) and (K2_C, K1_A)
            foreach (var @event in events)
            {
                output.AddRange(await _operator.OperateOnEvent(@event));
            }
            
            Assert.IsNotEmpty(output);
            var outputEvent = output.First() as TestEvent2;
            output.RemoveAt(0);
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(2, outputEvent.Value);
            Assert.AreEqual(11, outputEvent.Key);
            
            Assert.IsNotEmpty(output);
            outputEvent = output.First() as TestEvent2;
            output.RemoveAt(0);
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(2, outputEvent.Value);
            Assert.AreEqual(10001, outputEvent.Key);
            Assert.IsEmpty(output);

        }

        [Test]
        public async Task JoinOperator_ShouldNotJoinWithEventsOutOfWindow()
        {
            var output = new List<IEvent>();

            output.AddRange(await _operator.OperateOnEvent(new TestEvent { Key = 1, Value = 2 }));
            await Task.Delay(_windowSize);

            output.AddRange(await _operator.OperateOnEvent(new TestEvent2 { Key = 10, Value = 2 }));
            output.AddRange(await _operator.OperateOnEvent(new TestEvent { Key = 100, Value = 2 }));
            
            //second two events happen delayed to cause the first event to go out of window 
            //so should only return one match

            Assert.IsNotEmpty(output);
            var outputEvent = output.First() as TestEvent2;
            Assert.IsNotNull(outputEvent);
            Assert.AreEqual(4, outputEvent.Value);
            Assert.AreEqual(110, outputEvent.Key);
            
            output.RemoveAt(0);
            Assert.IsEmpty(output);
        }

        [TearDown]
        public void TearDown()
        {
            _operator.Dispose();
        }
    }
}
