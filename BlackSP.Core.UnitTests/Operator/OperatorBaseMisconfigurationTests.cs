using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Events;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Operator
{
    public class OperatorSocketBaseMisconfigurationTests
    {
        class NullBaseOperatorSocket : TestBaseOperatorSocket
        {
            protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
            {
                return null;
            }
        }

        class ExceptionBaseOperatorSocket : TestBaseOperatorSocket
        {
            protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void Operator_ThrowsOnNullOperationResult()
        {
            var testoperator = new NullBaseOperatorSocket();
            var operatorThread = testoperator.Start(DateTime.Now);
            testoperator.Enqueue(new TestEvent());
            Assert.ThrowsAsync<NullReferenceException>(async () => await operatorThread);
            //also assert that after exception on operating thread the internal state is cancelled
            Assert.Throws<OperationCanceledException>(() => testoperator.Enqueue(new TestEvent()));
        }

        [Test]
        public void Operator_ThrowsOnExceptionOperationResult()
        {
            var testoperator = new ExceptionBaseOperatorSocket();
            var operatorThread = testoperator.Start(DateTime.Now);
            testoperator.Enqueue(new TestEvent());
            Assert.ThrowsAsync<NotImplementedException>(async () => await operatorThread);

            //also assert that after an exception on the operating thread the operator has been cancelled and stopped processing
            Assert.Throws<OperationCanceledException>(() => testoperator.Enqueue(new TestEvent()));
        }
    }
}
