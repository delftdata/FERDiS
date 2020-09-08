using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using BlackSP.Infrastructure.Modules;
using BlackSP.Infrastructure.UnitTests.Events;
using BlackSP.OperatorShells;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.UnitTests
{
    class TestOperatorGraphBuilder : OperatorVertexGraphBuilderBase
    {
        protected override Task<IApplication> BuildGraph()
        {
            return Task.FromResult<IApplication>(null);
        }
    }

    public class OperatorGraphBuilderTests
    {
        private TestOperatorGraphBuilder graphBuilder; //expose internal api
        private IOperatorVertexGraphBuilder publicGraphBuilder => graphBuilder; //expose public api
        
        [SetUp]
        public void Setup()
        {
            graphBuilder = new TestOperatorGraphBuilder();
        }

        [Test]
        public async Task GraphConstruction_NoDuplicationOfImportantKeys()
        {
            var source = publicGraphBuilder.AddSource<SampleSourceOperator, EventA>(1);

            var filter = publicGraphBuilder.AddFilter<SampleFilterOperator, EventA>(1);
            source.Append(filter);
            //filter.Append(source); // this wont compile, awesome

            var map = publicGraphBuilder.AddMap<SampleMapOperator, EventA, EventB>(1);

            filter.Append(map);
            //map.Append(filter); //this wont compile, awesome

            var join = publicGraphBuilder.AddJoin<SampleJoinOperator, EventA, EventB, EventC>(1);
            map.Append(join);
            filter.Append(join);
            //join.Append(map); //this wont compile, awesome
            
            var aggregate = publicGraphBuilder.AddAggregate<SampleAggregateOperator, EventC, EventD>(1);
            join.Append(aggregate);
            //aggregate.Append(join); //this wont compile, awesome

            var sink = publicGraphBuilder.AddSink<SampleSinkOperator, EventD>(1);
            aggregate.Append(sink);
            //sink does not have append method, awesome

            var logConfig = new LogConfiguration();
            var cpConfig = new CheckpointConfiguration(0,false);
            await graphBuilder.Build(logConfig, cpConfig); //ensure complete (will add coordinator)

            //Asserts
            var usedNames = new HashSet<string>();
            foreach(var configurator in graphBuilder.VertexBuilders)
            {
                foreach(var instanceName in configurator.InstanceNames)
                {
                    Assert.IsFalse(usedNames.Contains(instanceName), "Duplicate instancename returned");
                    usedNames.Add(instanceName);
                }
                Assert.IsFalse(usedNames.Contains(configurator.VertexName), "Duplicate operatorname returned");
                usedNames.Add(configurator.VertexName);
            }

            var coordinatorModuleType = graphBuilder.VertexBuilders.FirstOrDefault(x => x.VertexType == Kernel.Models.VertexType.Coordinator).ModuleType;
            Assert.AreEqual(typeof(CoordinatorModule), coordinatorModuleType);

        }

        [Test]
        public void Source_CorrectConfiguration()
        {
            var source = publicGraphBuilder.AddSource<SampleSourceOperator, EventA>(1);

            Assert.AreEqual(typeof(SourceOperatorModule<SourceOperatorShell<EventA>, SampleSourceOperator, EventA>), source.ModuleType);
        }
        
        [Test]
        public void Filter_CorrectConfiguration()
        {
            var filter = publicGraphBuilder.AddFilter<SampleFilterOperator, EventA>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<FilterOperatorShell<EventA>, SampleFilterOperator>), filter.ModuleType);
        }

        [Test]
        public void Map_CorrectConfiguration()
        {
            var map = publicGraphBuilder.AddMap<SampleMapOperator, EventA, EventB>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<MapOperatorShell<EventA, EventB>, SampleMapOperator>), map.ModuleType);
        }

        [Test]
        public void Join_CorrectConfiguration()
        {
            var join = publicGraphBuilder.AddJoin<SampleJoinOperator, EventA, EventB, EventC>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<JoinOperatorShell<EventA, EventB, EventC>, SampleJoinOperator>), join.ModuleType);
        }

        [Test]
        public void Aggregate_CorrectConfiguration()
        {
            var aggregate = publicGraphBuilder.AddAggregate<SampleAggregateOperator, EventC, EventD>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<AggregateOperatorShell<EventC, EventD>, SampleAggregateOperator>), aggregate.ModuleType);
        }

        [Test]
        public void Sink_CorrectConfiguration()
        {
            var sink = publicGraphBuilder.AddSink<SampleSinkOperator, EventD>(1);
            Assert.AreEqual(typeof(ReactiveOperatorModule<SinkOperatorShell<EventD>, SampleSinkOperator>), sink.ModuleType);
        }
    }
}