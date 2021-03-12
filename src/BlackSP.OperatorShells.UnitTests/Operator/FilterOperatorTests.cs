using BlackSP.Core.UnitTests.Events;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells.UnitTests.Operator
{
    class FilterOperatorConfigurationNoDoubleKeys : IFilterOperator<TestEvent>
    {
        private IList<string> previousKeys;
        public FilterOperatorConfigurationNoDoubleKeys()
        {
            //a nasty stateful operator configuration that will surely go out of memory but its fine for tests
            previousKeys = new List<string>();
        }

        public TestEvent Filter(TestEvent @event)
        {
            if(previousKeys.Contains(@event.Key)) {
                return null;
            }
            previousKeys.Add(@event.Key);
            return @event;
        }
    }

    public class FilterOperatorTests
    {
        private FilterOperatorShell<TestEvent> _distinctOperator;
        private IList<IEvent> _testEvents;

        [SetUp]
        public void SetUp()
        {
            _distinctOperator = new FilterOperatorShell<TestEvent>(new FilterOperatorConfigurationNoDoubleKeys());
            _testEvents = new List<IEvent>();
            for(int i = 0; i < 10; i++)
            {
                _testEvents.Add(new TestEvent() { Key = $"K{i}", Value = (byte)i });
            }
        }

        [Test]
        public async Task FilterOperator_DistinctUsesLocalStateToFilterDuplicates()
        {
            var results = new List<IEvent>();
            foreach (var e in _testEvents)
            {
                results.AddRange(await _distinctOperator.OperateOnEvent(e));
                results.AddRange(await _distinctOperator.OperateOnEvent(e));//Add the events twice, so the seconds can get filtered
            }
            Assert.AreEqual(results.Count(), _testEvents.Count());
            foreach (var e in _testEvents)
            {
                Assert.AreEqual(e, results.First());
                results.RemoveAt(0);
            }
            Assert.AreEqual(results.Count(), 0);

        }

        [TearDown]
        public void TearDown()
        {
            _distinctOperator.Dispose();
        }
    }
}
