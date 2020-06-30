using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Kernel.Models;
using BlackSP.OperatorShells;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSP.Core.UnitTests.Operator
{
    public class FilterOperatorMisconfigurationTests
    {
        private FilterOperatorShell<TestEvent> _distinctOperator;
        private Task _operatorThread;

        [SetUp]
        public void SetUp()
        {
            _distinctOperator = new FilterOperatorShell<TestEvent>(new FilterOperatorConfigurationNoDoubleKeys());
            //_operatorThread = _distinctOperator.Start(DateTime.Now);

        }

        [Test]
        public async Task FilterOperator_ThrowsOnUnexpectedType()
        {
            var mockedOutputQueue = new Queue<IEvent>();
            var outputEndpoint = MockBuilder.MockOutputEndpoint(mockedOutputQueue);
            //_distinctOperator.RegisterOutputEndpoint(outputEndpoint.Object);

            _distinctOperator.OperateOnEvent(new TestEvent2()); //enqueue unexpected event type

            await Task.Delay(1); //give background thread some time to perform the operation
            Assert.ThrowsAsync<ArgumentException>(async () => await _operatorThread);
        }

        [TearDown]
        public void TearDown()
        {
            //Assert.ThrowsAsync<ArgumentException>(_distinctOperator.Stop);

            //_distinctOperator.Dispose();
        }
    }
}
