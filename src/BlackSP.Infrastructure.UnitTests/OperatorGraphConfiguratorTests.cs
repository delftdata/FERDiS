using BlackSP.OperatorShells;
using BlackSP.CRA.UnitTests.Events;
using BlackSP.Infrastructure.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackSP.Infrastructure.Modules;
using System.Linq;

namespace BlackSP.CRA.UnitTests
{
    class TestOperatorGraphBuilder : OperatorGraphBuilderBase
    {
        protected override Task<object> BuildGraph()
        {
            return Task.FromResult<object>(null);
        }
    }

    public class OperatorGraphBuilderTests
    {
        private TestOperatorGraphBuilder configurator;
        private IOperatorGraphBuilder publicConfigurator => configurator;
        [SetUp]
        public void Setup()
        {
            configurator = new TestOperatorGraphBuilder();
        }

        [Test]
        public async Task GraphConstruction_NoDuplicationOfImportantKeys()
        {
            var source = publicConfigurator.AddSource<SampleSourceOperator, EventA>(1);

            var filter = publicConfigurator.AddFilter<SampleFilterOperator, EventA>(1);
            source.Append(filter);
            //filter.Append(source); // this wont compile, awesome

            var map = publicConfigurator.AddMap<SampleMapOperator, EventA, EventB>(1);

            filter.Append(map);
            //map.Append(filter); //this wont compile, awesome

            var join = publicConfigurator.AddJoin<SampleJoinOperator, EventA, EventB, EventC>(1);
            map.Append(join);
            filter.Append(join);
            //join.Append(map); //this wont compile, awesome
            
            var aggregate = publicConfigurator.AddAggregate<SampleAggregateOperator, EventC, EventD>(1);
            join.Append(aggregate);
            //aggregate.Append(join); //this wont compile, awesome

            var sink = publicConfigurator.AddSink<SampleSinkOperator, EventD>(1);
            aggregate.Append(sink);
            //sink does not have append method, awesome

            await configurator.Build(); //ensure complete (will add coordinator)

            //Asserts
            var usedNames = new HashSet<string>();
            foreach(var configurator in configurator.Configurators)
            {
                foreach(var instanceName in configurator.InstanceNames)
                {
                    Assert.IsFalse(usedNames.Contains(instanceName), "Duplicate instancename returned");
                    usedNames.Add(instanceName);
                }
                Assert.IsFalse(usedNames.Contains(configurator.VertexName), "Duplicate operatorname returned");
                usedNames.Add(configurator.VertexName);
            }

            var coordinatorModuleType = configurator.Configurators.Last().ModuleType;
            Assert.AreEqual(typeof(CoordinatorModule), coordinatorModuleType);

        }

        [Test]
        public void Source_CorrectConfiguration()
        {
            var source = publicConfigurator.AddSource<SampleSourceOperator, EventA>(1);

            Assert.AreEqual(typeof(SourceOperatorModule<SourceOperatorShell<EventA>, SampleSourceOperator, EventA>), source.ModuleType);
        }
        
        [Test]
        public void Filter_CorrectConfiguration()
        {
            var filter = publicConfigurator.AddFilter<SampleFilterOperator, EventA>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<FilterOperatorShell<EventA>, SampleFilterOperator>), filter.ModuleType);
        }

        [Test]
        public void Map_CorrectConfiguration()
        {
            var map = publicConfigurator.AddMap<SampleMapOperator, EventA, EventB>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<MapOperatorShell<EventA, EventB>, SampleMapOperator>), map.ModuleType);
        }

        [Test]
        public void Join_CorrectConfiguration()
        {
            var join = publicConfigurator.AddJoin<SampleJoinOperator, EventA, EventB, EventC>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<JoinOperatorShell<EventA, EventB, EventC>, SampleJoinOperator>), join.ModuleType);
        }

        [Test]
        public void Aggregate_CorrectConfiguration()
        {
            var aggregate = publicConfigurator.AddAggregate<SampleAggregateOperator, EventC, EventD>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<AggregateOperatorShell<EventC, EventD>, SampleAggregateOperator>), aggregate.ModuleType);
        }

        [Test]
        public void Sink_CorrectConfiguration()
        {
            var sink = publicConfigurator.AddSink<SampleSinkOperator, EventD>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<SinkOperatorShell<EventD>, SampleSinkOperator>), sink.ModuleType);
        }
    }
}