using BlackSP.OperatorShells;
using BlackSP.Core.UnitTests.Events;
using BlackSP.Core.UnitTests.Utilities;
using BlackSP.Kernel.Models;
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
    class BaseOperator : IOperator
    {}

    class TestBaseOperatorShell : OperatorShellBase
    {
        public TestBaseOperatorShell() : base(new BaseOperator())
        {}

        public override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            yield return @event; //base operator that just passes on events
        }
    }

    public class OperatorShellBaseTests
    {
        private OperatorShellBase _operator;
        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _operator = new TestBaseOperatorShell();

            _testEvents = new List<IEvent>();
            for (int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }
        }

        [Test]
        public void OperateOnEvent_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => _operator.OperateOnEvent(null));
        }

        [Test]
        public async Task Operator_PassesAnEventThrough()
        {
            var results = _operator.OperateOnEvent(_testEvents[0]);

            Assert.IsTrue(results.Any());
            Assert.AreEqual(_testEvents[0], results.First());
        }

        [Test]
        public async Task Operator_PassesEventsThroughInOrder()
        {
            foreach (var e in _testEvents)
            {
                _operator.OperateOnEvent(e);
            }

            var results = _testEvents.SelectMany(e => _operator.OperateOnEvent(e)).ToArray();

            Assert.IsTrue(results.Any());
            Assert.IsTrue(Enumerable.SequenceEqual(_testEvents, results));
        }

        [TearDown]
        public void TearDown()
        {
            _operator.Dispose();
        }
    }
}
