using BlackSP.Core.Operators;
using BlackSP.Core.Operators.Concrete;
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
    public class FilterOperatorMisconfigurationTests
    {
        private FilterOperator<TestEvent> _distinctOperator;
        private Task _operatorThread;

        [SetUp]
        public void SetUp()
        {
            _distinctOperator = new FilterOperator<TestEvent>(new FilterOperatorConfigurationNoDoubleKeys());
            _operatorThread = _distinctOperator.Start(DateTime.Now);

        }

        [Test]
        public async Task FilterOperator_ThrowsOnUnexpectedType()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            _distinctOperator.RegisterOutputEndpoint(outputEndpoint.Object);

            _distinctOperator.Enqueue(new TestEvent2()); //enqueue unexpected event type

            await Task.Delay(1); //give background thread some time to perform the operation
            Assert.ThrowsAsync<ArgumentException>(async () => await _operatorThread);
        }

        [TearDown]
        public void TearDown()
        {
            Assert.ThrowsAsync<ArgumentException>(_distinctOperator.Stop);

            _distinctOperator.Dispose();
        }
    }
}
