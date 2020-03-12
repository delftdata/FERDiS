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
    class MapOperatorConfigurationForTest : IMapOperatorConfiguration<TestEvent, TestEvent2>
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
        private IOperator _mapOperator;

        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _mapOperator = new MapOperator<TestEvent, TestEvent2>(new MapOperatorConfigurationForTest());

            _testEvents = new List<IEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }

        }

        [Test]
        public async Task MapOperator_TransformsAnEvent()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _mapOperator.RegisterOutputEndpoint(outputEndpoint.Object);
            
            var operatorThread = _mapOperator.Start();

            _mapOperator.Enqueue(_testEvents[0]);
            
            await Task.Delay(50); //give background thread some time to perform the operation
            Assert.ThrowsAsync<OperationCanceledException>(_mapOperator.Stop);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await operatorThread);

            Assert.IsTrue(mockedOutputQueue.Any());
            
            var transformedEvent = mockedOutputQueue.Dequeue() as TestEvent2;
            Assert.IsNotNull(transformedEvent);

            Assert.IsTrue(transformedEvent.Key.StartsWith("Transformed"));
            Assert.AreEqual((_testEvents[0] as TestEvent).Value, transformedEvent.Value);//byte transformed to int
        }

    }
}
