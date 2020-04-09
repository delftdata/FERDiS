using BlackSP.CRA.Configuration;
using BlackSP.CRA.Kubernetes;
using BlackSP.CRA.UnitTests.Events;
using CRA.ClientLibrary;
using CRA.DataProvider;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlackSP.CRA.UnitTests
{
    public class OperatorGraphConfiguratorTests
    {
        private OperatorGraphConfigurator configurator;
        private IOperatorGraphConfigurator publicConfigurator => configurator;
        [SetUp]
        public void Setup()
        {
            // Note: due to terrible testability of CRA
            //       the choice has been made to not test these components of the system
            //       it would require refactoring the original project which does not
            //       fit in my schedule.

            //var craVertexProvider = new Mock<IVertexInfoProvider>();
            //craVertexProvider.SetReturnsDefault(Task.CompletedTask);
            //var craEndpointProvider = new Mock<IEndpointInfoProvider>();
            //craEndpointProvider.SetReturnsDefault(Task.CompletedTask);
            //var craConnectionProvider = new Mock<IVertexConnectionInfoProvider>();
            //craConnectionProvider.SetReturnsDefault(Task.CompletedTask);
            //var craShardProvider = new Mock<IShardedVertexInfoProvider>();
            //craShardProvider.SetReturnsDefault(Task.CompletedTask);

            var craDataProvider = new Mock<IDataProvider>();
            //craDataProvider.Setup(x => x.GetVertexInfoProvider()).Returns(craVertexProvider.Object);
            //craDataProvider.Setup(x => x.GetEndpointInfoProvider()).Returns(craEndpointProvider.Object);
            //craDataProvider.Setup(x => x.GetVertexConnectionInfoProvider()).Returns(craConnectionProvider.Object);
            //craDataProvider.Setup(x => x.GetShardedVertexInfoProvider()).Returns(craShardProvider.Object);

            //Below behavior is not overridable due to CRAClientLibrary being a purely concrete class. (needs interface)
            var craClientLibrary = new Mock<CRAClientLibrary>(craDataProvider.Object);
            //craClientLibrary.Setup(x => x.DefineVertexAsync(It.IsAny<string>(), It.IsAny<Expression<Func<IShardedVertex>>>()))
            //    .Returns(Task.FromResult(CRAErrorCode.Success));
            //craClientLibrary.Setup(x => x.InstantiateVertexAsync(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), null))
            //    .Returns(Task.FromResult(CRAErrorCode.Success));
            //craClientLibrary.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            //    .Returns(Task.FromResult(CRAErrorCode.Success));

            var kubernetesUtility = new Mock<KubernetesDeploymentUtility>();
            kubernetesUtility.Setup(x => x.WriteDeploymentYaml());

            configurator = new OperatorGraphConfigurator(kubernetesUtility.Object, craClientLibrary.Object);
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
    }
}