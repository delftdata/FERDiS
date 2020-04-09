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
            var craDataProvider = new Mock<IDataProvider>();
            var craClientLibrary = new Mock<CRAClientLibrary>(craDataProvider.Object);
            craClientLibrary.Setup(x => x.DefineVertexAsync(It.IsAny<string>(), It.IsAny<Expression<Func<IShardedVertex>>>()))
                .Returns(Task.FromResult(CRAErrorCode.Success));
            craClientLibrary.Setup(x => x.InstantiateVertexAsync(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), null))
                .Returns(Task.FromResult(CRAErrorCode.Success));
            craClientLibrary.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(CRAErrorCode.Success));
            //setup definevertexasync
            //setup instantiatevertexasync
            //setup connectasync
            var kubernetesUtility = new Mock<KubernetesDeploymentUtility>();
            kubernetesUtility.Setup(x => x.WriteDeploymentYaml());

            configurator = new OperatorGraphConfigurator(kubernetesUtility.Object, craClientLibrary.Object);
        }

        [Test]
        public async Task Graph()
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

            await configurator.BuildGraph();

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