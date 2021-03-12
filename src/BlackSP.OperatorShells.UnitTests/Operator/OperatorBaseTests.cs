using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Extensions;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    class BaseOperator : IOperator
    {}

    class TestBaseOperatorShell : OperatorShellBase
    {
        public TestBaseOperatorShell() : base(new BaseOperator())
        {}

        public override async Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event)
        {
            return @event.Yield(); //base operator that just passes on events
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
        public async Task Operator_PassesAnEventThrough()
        {
            var results = await _operator.OperateOnEvent(_testEvents[0]);

            Assert.IsTrue(results.Any());
            Assert.AreEqual(_testEvents[0], results.First());
        }

        [Test]
        public async Task Operator_PassesEventsThroughInOrder()
        {
            foreach (var e in _testEvents)
            {
                await _operator.OperateOnEvent(e);
            }

            var results = (await Task.WhenAll(_testEvents.Select(e => _operator.OperateOnEvent(e)))).SelectMany(res => res);

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
