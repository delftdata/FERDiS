using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    public class OperatorShellBaseMisconfigurationTests
    {
        class ExceptionBaseOperatorShell : TestBaseOperatorShell
        {
            public override Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void Operator_ThrowsOnExceptionOperationResult()
        {
            var testoperator = new ExceptionBaseOperatorShell();            
            Assert.Throws<NotImplementedException>(() => testoperator.OperateOnEvent(new TestEvent()));
        }
    }
}
