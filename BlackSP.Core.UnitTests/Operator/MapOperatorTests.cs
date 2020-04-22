using BlackSP.Core.OperatorShells;
using BlackSP.Core.OperatorShells;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Kernel.Endpoints;
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
    class MapOperatorConfigurationForTest : IMapOperator<TestEvent, TestEvent2>
    {
        public IEnumerable<TestEvent2> Map(TestEvent @event)
        {
            yield return new TestEvent2
            {
                Key = $"Transformed:{@event.Key}",
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
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }

            _operatorThread = _mapOperator.Start(DateTime.Now);

        }

        [Test]
        public async Task MapOperator_TransformsAnEvent()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _mapOperator.RegisterOutputEndpoint(outputEndpoint.Object);
            
            //put one event in the operator input queue
            _mapOperator.Enqueue(_testEvents[0]);
            
            await Task.Delay(1); //give background thread some time to perform the operation
            
            Assert.IsTrue(mockedOutputQueue.Any());
            
            var transformedEvent = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.IsNotNull(transformedEvent);

            Assert.IsTrue(transformedEvent.Key.StartsWith("Transformed"));
            Assert.AreEqual(_testEvents[0].Value, transformedEvent.Value);//byte transformed to int
        }

        [Test]
        public async Task MapOperator_TransformsMultipleEvents()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _mapOperator.RegisterOutputEndpoint(outputEndpoint.Object);

            foreach(var e in _testEvents)
            {
                _mapOperator.Enqueue(e);
            }

            await Task.Delay(1); //give background thread some time to perform the operation

            foreach(var e in _testEvents)
            {
                Assert.IsTrue(mockedOutputQueue.Any());
                var transformedEvent = mockedOutputQueue.Dequeue() as TestEvent2;
                Assert.IsNotNull(transformedEvent);
                Assert.IsTrue(transformedEvent.Key.StartsWith("Transformed"));
                Assert.AreEqual(e.Value, transformedEvent.Value);//byte transformed to int
            }
            
        }

        [TearDown]
        public void TearDown()
        {
            Assert.ThrowsAsync<OperationCanceledException>(_mapOperator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await _operatorThread);

            _mapOperator.Dispose();
        }

    }
}
