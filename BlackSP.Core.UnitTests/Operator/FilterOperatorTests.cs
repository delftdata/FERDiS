﻿using BlackSP.Core.Operators;
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
    class FilterOperatorConfigurationNoDoubleKeys : IFilterOperatorConfiguration<TestEvent>
    {
        private IList<string> previousKeys;
        public FilterOperatorConfigurationNoDoubleKeys()
        {
            //a nasty stateful operator configuration that will surely go out of memory but its fine for tests
            previousKeys = new List<string>();
        }

        public TestEvent Filter(TestEvent @event)
        {
            if(previousKeys.Contains(@event.Key)) {
                return null;
            }
            previousKeys.Add(@event.Key);
            return @event;
        }
    }

    public class FilterOperatorTests
    {
        private IOperator _distinctOperator;

        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _distinctOperator = new FilterOperator<TestEvent>(new FilterOperatorConfigurationNoDoubleKeys());
            _testEvents = new List<IEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }
        }

        [Test]
        public async Task FilterOperator_DistinctUsesLocalStateToFilterDuplicates()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _distinctOperator.RegisterOutputEndpoint(outputEndpoint.Object);

            var operatorThread = _distinctOperator.Start();

            foreach (var e in _testEvents)
            {
                _distinctOperator.Enqueue(e);
                _distinctOperator.Enqueue(e);//Add the events twice, so the seconds can get filtered
            }

            await Task.Delay(50); //give background thread some time to perform the operation
            Assert.ThrowsAsync<OperationCanceledException>(_distinctOperator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operatorThread);

            Assert.IsTrue(mockedOutputQueue.Any());
            foreach (var e in _testEvents)
            {
                Assert.AreEqual(e, mockedOutputQueue.Dequeue());
            }
            Assert.IsFalse(mockedOutputQueue.Any()); //crucial statement for this test, the queue should be empty at this point to reflect the filtered out events
        }

        [Test]
        public async Task FilterOperator_ThrowsOnUnexpectedType()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _distinctOperator.RegisterOutputEndpoint(outputEndpoint.Object);

            var operatorThread = _distinctOperator.Start();
            _distinctOperator.Enqueue(new TestEvent2()); //enqueue unexpected event type

            await Task.Delay(50); //give background thread some time to perform the operation
            Assert.ThrowsAsync<ArgumentException>(async () => await operatorThread);
        }

    }
}