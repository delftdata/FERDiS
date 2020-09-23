using BlackSP.Core.UnitTests.Events;
using BlackSP.OperatorShells;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    public class FilterOperatorMisconfigurationTests
    {
        private FilterOperatorShell<TestEvent> _distinctOperator;

        [SetUp]
        public void SetUp()
        {
            _distinctOperator = new FilterOperatorShell<TestEvent>(new FilterOperatorConfigurationNoDoubleKeys());

        }

        [Test]
        public void FilterOperator_ThrowsOnUnexpectedType()
        {
            Assert.Throws<ArgumentException>(() => 
            _distinctOperator.OperateOnEvent(new TestEvent2()).ToArray() //ensure materialisation of results
            ); //enqueue unexpected event type
        }

        [TearDown]
        public void TearDown()
        {
        }
    }
}
