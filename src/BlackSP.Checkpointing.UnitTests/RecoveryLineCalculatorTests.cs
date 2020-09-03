using BlackSP.Checkpointing.Core;
using BlackSP.Kernel.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Checkpointing.UnitTests
{
    class RecoveryLineCalculatorTests
    {
        
        private RecoveryLineCalculator calculator;

        [SetUp]
        public void SetUp()
        {
            var names = new List<string>();
            names.Add("instance1");
            names.Add("instance2");
            names.Add("instance3");
            var connections = new List<Tuple<string, string>>();
            connections.Add(Tuple.Create(names[0], names[1]));
            connections.Add(Tuple.Create(names[1], names[2]));

            var graphConfigMock = new Mock<IVertexGraphConfiguration>();
            graphConfigMock.Setup(config => config.InstanceNames).Returns(names);
            graphConfigMock.Setup(config => config.InstanceConnections).Returns(connections);

            var metas = new List<MetaData>();
            metas.Add(new MetaData(Guid.NewGuid(), new Dictionary<string, Guid>(), names[0], DateTime.Now.AddMinutes(-10)));

            var dependencies = new Dictionary<string, Guid>();
            dependencies.Add(names[0], metas[0].Id);
            metas.Add(new MetaData(Guid.NewGuid(), dependencies, names[1], DateTime.Now.AddMinutes(-9)));

            dependencies = new Dictionary<string, Guid>();
            dependencies.Add(names[1], metas[1].Id);
            metas.Add(new MetaData(Guid.NewGuid(), dependencies, names[2], DateTime.Now.AddMinutes(-9)));

            calculator = new RecoveryLineCalculator(metas, graphConfigMock.Object);
        }

        [Test]
        public void SourceFailure_ShouldRollbackToInitialState()
        {
            var recoveryLine = calculator.CalculateRecoveryLine(Enumerable.Repeat("instance1", 1));
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), 1); //in this test all instances roll back to their initial state except the source which can recover its last checkpoint
            Assert.Contains("instance1", recoveryLine.AffectedWorkers.ToList());
        }

        [Test]
        public void CenterFailure_ShouldRollbackDownstream()
        {
            var recoveryLine = calculator.CalculateRecoveryLine(Enumerable.Repeat("instance2", 1));
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), 1);
            Assert.Contains("instance2", recoveryLine.AffectedWorkers.ToList());
            //instance 1 remains unaffected due to its runtime state still being valid
            //instance 3 remains unaffected due to the recovery line being at instance 3 its initial state
        }

        [Test]
        public void SinkFailure_ShouldOnlyRollbackSink()
        {
            var recoveryLine = calculator.CalculateRecoveryLine(Enumerable.Repeat("instance3", 1));
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), 1);
            //instance 1 and 2 remain unaffected due to its runtime state still being valid
            Assert.Contains("instance3", recoveryLine.AffectedWorkers.ToList());

        }

    }
}
