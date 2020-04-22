using BlackSP.Core.OperatorShells;
using BlackSP.CRA.UnitTests.Events;
using BlackSP.Infrastructure.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSP.CRA.UnitTests
{
    class TestOperatorGraphBuilder : OperatorGraphBuilderBase
    {
        public override Task<object> BuildGraph()
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

            //Dont build the actual graph, that will attempt to invoke CRA methods which we cant mock :/
            //await configurator.BuildGraph();

            //Asserts
            var usedNames = new HashSet<string>();
            foreach(var configurator in configurator.Configurators)
            {
                foreach(var instanceName in configurator.InstanceNames)
                {
                    Assert.IsFalse(usedNames.Contains(instanceName), "Duplicate instancename returned");
                    usedNames.Add(instanceName);
                }
                Assert.IsFalse(usedNames.Contains(configurator.OperatorName), "Duplicate operatorname returned");
                usedNames.Add(configurator.OperatorName);
            }
        }

        [Test]
        public void Source_CorrectConfiguration()
        {
            var source = publicConfigurator.AddSource<SampleSourceOperator, EventA>(1);
            Assert.AreEqual(typeof(SampleSourceOperator), source.OperatorConfigurationType);
            Assert.AreEqual(typeof(SourceOperatorShell<EventA>), source.OperatorType);
        }

        [Test]
        public void Filter_CorrectConfiguration()
        {
            var filter = publicConfigurator.AddFilter<SampleFilterOperator, EventA>(1);
            Assert.AreEqual(typeof(SampleFilterOperator), filter.OperatorConfigurationType);
            Assert.AreEqual(typeof(FilterOperatorShell<EventA>), filter.OperatorType);
        }

        [Test]
        public void Map_CorrectConfiguration()
        {
            var map = publicConfigurator.AddMap<SampleMapOperator, EventA, EventB>(1);
            Assert.AreEqual(typeof(SampleMapOperator), map.OperatorConfigurationType);
            Assert.AreEqual(typeof(MapOperatorShell<EventA, EventB>), map.OperatorType);
        }

        [Test]
        public void Join_CorrectConfiguration()
        {
            var join = publicConfigurator.AddJoin<SampleJoinOperator, EventA, EventB, EventC>(1);
            Assert.AreEqual(typeof(SampleJoinOperator), join.OperatorConfigurationType);
            Assert.AreEqual(typeof(JoinOperatorShell<EventA, EventB, EventC>), join.OperatorType);
        }

        [Test]
        public void Aggregate_CorrectConfiguration()
        {
            var aggregate = publicConfigurator.AddAggregate<SampleAggregateOperator, EventC, EventD>(1);
            Assert.AreEqual(typeof(SampleAggregateOperator), aggregate.OperatorConfigurationType);
            Assert.AreEqual(typeof(AggregateOperatorShell<EventC, EventD>), aggregate.OperatorType);
        }

        [Test]
        public void Sink_CorrectConfiguration()
        {
            var sink = publicConfigurator.AddSink<SampleSinkOperator, EventD>(1);
            Assert.AreEqual(typeof(SampleSinkOperator), sink.OperatorConfigurationType);
            Assert.AreEqual(typeof(SinkOperatorShell<EventD>), sink.OperatorType);
        }
    }
}