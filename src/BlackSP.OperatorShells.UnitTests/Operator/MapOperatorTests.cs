﻿using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    class MapOperatorConfigurationForTest : IMapOperator<TestEvent, TestEvent2>
    {
        public IEnumerable<TestEvent2> Map(TestEvent @event)
        {
            yield return new TestEvent2
            {
                Key = @event.Key,
                Value = (int)@event.Value //transform value from byte to int32
            }; 
        }
    }


    public class MapOperatorTests
    {
        private MapOperatorShell<TestEvent, TestEvent2> _mapOperator;
        private Task _operatorThread;
        private IList<TestEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _mapOperator = new MapOperatorShell<TestEvent, TestEvent2>(new MapOperatorConfigurationForTest());

            _testEvents = new List<TestEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = i, Value = (byte)i });
            }
        }

        [Test]
        public async Task MapOperator_TransformsAnEvent()
        {
            //operate on one event
            var output = await _mapOperator.OperateOnEvent(_testEvents[0]);           
            Assert.IsTrue(output.Any());
            
            var transformedEvent = output.First() as TestEvent2;
            Assert.IsNotNull(transformedEvent);

            Assert.AreEqual(_testEvents[0].Key, transformedEvent.Key);
            Assert.AreEqual(_testEvents[0].Value, transformedEvent.Value);//byte transformed to int
        }

        [Test]
        public async Task MapOperator_TransformsMultipleEvents()
        {
            var output = new List<IEvent>();

            foreach(var e in _testEvents)
            {
                output.AddRange(await _mapOperator.OperateOnEvent(e));
            }

            foreach(var e in _testEvents)
            {
                Assert.IsTrue(output.Any());
                var transformedEvent = output.First() as TestEvent2;
                output.RemoveAt(0);//dequeue

                Assert.IsNotNull(transformedEvent);
                Assert.AreEqual(e.Key, transformedEvent.Key);
                Assert.AreEqual(e.Value, transformedEvent.Value);//byte transformed to int
            }
            
        }

        [TearDown]
        public void TearDown()
        {
            _mapOperator.Dispose();
        }

    }
}
