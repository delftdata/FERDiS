using BlackSP.Checkpointing.Core;
using BlackSP.Kernel.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Checkpointing.UnitTests.Recovery
{
    class RecoveryLineCalculatorLineGraphTests
    {
        private CheckpointingInstanceGraphTestUtility utility;

        private readonly string instance1 = "instance1";
        private readonly string instance2 = "instance2";
        private readonly string instance3 = "instance3";

        private IDictionary<string, Guid> initialCheckpoints;
        private Guid instance2LatestCp;
        private Guid instance3LatestCp;

        [SetUp]
        public void SetUp()
        {
            utility = new CheckpointingInstanceGraphTestUtility();
            utility.AddInstance(instance1);
            utility.AddInstance(instance2);
            utility.AddInstance(instance3);

            utility.AddConnection(instance1, instance2);
            utility.AddConnection(instance2, instance3);

            initialCheckpoints = utility.instanceNames.ToDictionary(name => name, name => utility.AddCheckpoint(name, true));
            instance2LatestCp = utility.AddCheckpoint(instance2);
            instance3LatestCp = utility.AddCheckpoint(instance3);
            
        }

        [Test]
        public void SourceFailure_ShouldRollbackToInitialState()
        {
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance1);
            Assert.AreEqual(3, recoveryLine.AffectedWorkers.Count()); //in this test all instances roll back to their initial state
            Assert.AreEqual(initialCheckpoints[instance1], recoveryLine.RecoveryMap[instance1], instance1);
            Assert.AreEqual(initialCheckpoints[instance2], recoveryLine.RecoveryMap[instance2], instance2);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3], instance3);

        }

        [Test]
        public void CenterFailure_ShouldRollbackDownstream()
        {
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance2);
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), 3);
            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]); //guid.empty indicates runtime-state, so no rollback
            Assert.AreEqual(instance2LatestCp, recoveryLine.RecoveryMap[instance2]); //recovers latest checkpoint
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]); //recovers latest checkpoint that does not depend on instance2 (ie, initial cp)
            //instance 3 remains unaffected due to the recovery line being at instance 3 its initial state
        }

        [Test]
        public void SinkFailure_ShouldOnlyRollbackSink()
        {
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance3);
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), 3);
            //instance 1 and 2 remain unaffected due to its runtime state still being valid
            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(instance3LatestCp, recoveryLine.RecoveryMap[instance3]);


        }

    }
}
