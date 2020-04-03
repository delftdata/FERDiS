using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Events;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Operator
{
    public class BaseOperatorMisconfigurationTests
    {
        class NullBaseOperator : TestBaseOperator
        {
            protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
            {
                return null;
            }
        }

        class ExceptionBaseOperator : TestBaseOperator
        {
            protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void Operator_ThrowsOnNullOperationResult()
        {
            var testoperator = new NullBaseOperator();
            var operatorThread = testoperator.Start(DateTime.Now);
            testoperator.Enqueue(new TestEvent());
            Assert.ThrowsAsync<NullReferenceException>(async () => await operatorThread);
            //also assert that after exception on operating thread the internal state is cancelled
            Assert.Throws<OperationCanceledException>(() => testoperator.Enqueue(new TestEvent()));
        }

        [Test]
        public void Operator_ThrowsOnExceptionOperationResult()
        {
            var testoperator = new ExceptionBaseOperator();
            var operatorThread = testoperator.Start(DateTime.Now);
            testoperator.Enqueue(new TestEvent());
            Assert.ThrowsAsync<NotImplementedException>(async () => await operatorThread);

            //also assert that after an exception on the operating thread the operator has been cancelled and stopped processing
            Assert.Throws<OperationCanceledException>(() => testoperator.Enqueue(new TestEvent()));
        }
    }
}
