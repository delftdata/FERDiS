using BlackSP.Checkpointing.Core;
using BlackSP.Kernel.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Checkpointing.UnitTests.Recovery
{
    class RecoveryLineCalculatorCycleTests
    {
        private CheckpointingInstanceGraphTestUtility utility;

        private IDictionary<string, Guid> initialCheckpoints;

        private readonly string instance1 = "instance1";
        private readonly string instance2 = "instance2";
        private readonly string instance3 = "instance3";
        private readonly string instance4 = "instance4";
        private readonly int instanceCount = 4;

        [SetUp]
        public void SetUp()
        {
            utility = new CheckpointingInstanceGraphTestUtility();

            utility.AddInstance(instance1);
            utility.AddInstance(instance2);
            utility.AddInstance(instance3);
            utility.AddInstance(instance4);

            utility.AddConnection(instance1, instance2);
            utility.AddConnection(instance2, instance3); //note: cycle between instances 2 and 3
            utility.AddConnection(instance3, instance4);
            utility.AddConnection(instance3, instance2);

            initialCheckpoints = utility.instanceNames.ToDictionary(name => name, name => utility.AddCheckpoint(name, true));
        }

        [Test]
        public void SourceFailure_ShouldRollbackToInitialState()
        {
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance1);
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), instanceCount); //in this test all instances roll back to their initial state except the source which can recover its last checkpoint
            Assert.AreEqual(initialCheckpoints[instance1], recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(initialCheckpoints[instance2], recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);
        }

        [Test]
        public void SourceFailure_ShouldRollbackToLastCheckpoint_RestReturnsToInitialState()
        {
            _ = utility.AddCheckpoint(instance1);
            _ = utility.AddCheckpoint(instance1);
            var latestCp = utility.AddCheckpoint(instance1);
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance1);
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), instanceCount); //in this test all instances roll back to their initial state except the source which can recover its last checkpoint
            Assert.AreEqual(latestCp, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(initialCheckpoints[instance2], recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);
        }

        [Test]
        public void SourceFailure_CausesDominoInCycle()
        {
            _ = utility.AddCheckpoint(instance1);
            _ = utility.AddCheckpoint(instance2);
            _ = utility.AddCheckpoint(instance1);
            var latestCp2 = utility.AddCheckpoint(instance2);
            var latestCp1 = utility.AddCheckpoint(instance1);
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance1);
            
            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), instanceCount); //in this test all instances roll back to their initial state except the source which can recover its last checkpoint
            Assert.AreEqual(latestCp1, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(initialCheckpoints[instance2], recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);
        }

        [Test]
        public void SourceFailure_Not_CausesDominoInCycle_WithAlignedCheckpoints()
        {
            _ = utility.AddCheckpoint(instance1);
            _ = utility.AddCheckpoint(instance2); //align with cycle 
            _ = utility.AddCheckpoint(instance1);
            var latestCp2 = utility.ForceCheckpoint(instance2, instance3);
            var latestCp1 = utility.AddCheckpoint(instance1);
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance1);

            Assert.AreEqual(recoveryLine.AffectedWorkers.Count(), instanceCount); //in this test all instances roll back to their initial state except the source which can recover its last checkpoint
            Assert.AreEqual(latestCp1, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(latestCp2, recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);
        }

        [Test]
        public void Instance2Failure_ShouldRollbackToInitialState()
        {
            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance2);

            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(initialCheckpoints[instance2], recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);
            //instance 3 remains unaffected due to the recovery line being at instance 3 its initial state
        }

        [Test]
        public void Instance2Failure_CIC_CanCauseDomino()
        {
            _ = utility.AddCheckpoint(instance1);
            _ = utility.AddCheckpoint(instance2);
            _ = utility.AddCheckpoint(instance3);
            _ = utility.AddCheckpoint(instance4);


            _ = utility.AddCheckpoint(instance1);
            var instance2LastCp = utility.AddCheckpoint(instance2);
            var instance3LastCp = utility.AddCheckpoint(instance3);
            var instance4LastCp = utility.AddCheckpoint(instance4);

            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance2);

            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(initialCheckpoints[instance2], recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(initialCheckpoints[instance3], recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);
            //instance 3 remains unaffected due to the recovery line being at instance 3 its initial state
        }

        [Test]
        public void Instance3Failure_CIC_PropagatesDownstream()
        {
            var instance2LatestCp = utility.AddCheckpoint(instance2);
            var instance3LatestCp = utility.ForceCheckpoint(instance3, instance2);
            var instance4LatestCp = utility.AddCheckpoint(instance4); //note dependency on inst3 latest cp.

            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance3);

            //expect inst 3 and 4 affected, but also 2 as its on a cycle with 3
            Assert.AreEqual(3, recoveryLine.AffectedWorkers.Count());
            //instance 1 remains unaffected due to its runtime state still being valid
            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]);
            
            Assert.AreEqual(instance2LatestCp, recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(instance3LatestCp, recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(initialCheckpoints[instance4], recoveryLine.RecoveryMap[instance4]);

        }

        [Test]
        public void Instance3Failure_CIC_PropagatesDownstream_2()
        {
            var instance2LatestCp = utility.AddCheckpoint(instance2);
            var instance3LatestCp = utility.ForceCheckpoint(instance3, instance2);
            var instance4LatestCp = utility.ForceCheckpoint(instance4, instance3);

            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance3);

            //expect inst 3 and 4 affected, but also 2 as its on a cycle with 3
            Assert.AreEqual(3, recoveryLine.AffectedWorkers.Count());
            //instance 1 remains unaffected due to its runtime state still being valid
            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]);

            Assert.AreEqual(instance2LatestCp, recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(instance3LatestCp, recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(instance4LatestCp, recoveryLine.RecoveryMap[instance4]);

        }

        [Test]
        public void Instance1Failure_CIC_RestoresLastRecoveryLine()
        {
            _ = utility.AddCheckpoint(instance4);
            _ = utility.AddCheckpoint(instance3);
            _ = utility.AddCheckpoint(instance2);
            _ = utility.AddCheckpoint(instance1);

            var instance4LatestCp = utility.AddCheckpoint(instance4);
            var instance3LatestCp = utility.AddCheckpoint(instance3);
            var instance2LatestCp = utility.ForceCheckpoint(instance2, instance3);
            var instance1LatestCp = utility.AddCheckpoint(instance1);

            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            //note: no usage of future checkpoints when having coordinated checkpoints will affect the entire graph not just downstream
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance1);
            Assert.AreEqual(instance1LatestCp, recoveryLine.RecoveryMap[instance1]);
            Assert.AreEqual(instance2LatestCp, recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(instance3LatestCp, recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(instance4LatestCp, recoveryLine.RecoveryMap[instance4]);
        }

        [Test]
        public void Instance2Failure_CIC_RestoresLastRecoveryLine()
        {
            _ = utility.AddCheckpoint(instance4);
            _ = utility.AddCheckpoint(instance3);
            _ = utility.AddCheckpoint(instance2);
            _ = utility.AddCheckpoint(instance1);

            var instance4LatestCp = utility.AddCheckpoint(instance4);
            var instance3LatestCp = utility.AddCheckpoint(instance3);
            var instance2LatestCp = utility.ForceCheckpoint(instance2, instance3);
            var instance1LatestCp = utility.AddCheckpoint(instance1);

            var calculator = new RecoveryLineCalculator(utility.GetAllCheckpointMetaData(), utility.GetGraphConfig());
            //note: no usage of future checkpoints when having coordinated checkpoints will affect the entire graph not just downstream
            var recoveryLine = calculator.CalculateRecoveryLine(true, instance2);
            Assert.AreEqual(Guid.Empty, recoveryLine.RecoveryMap[instance1]); //instance 1 can re-use existing state (future-checkpoint) so will not rollback
            Assert.AreEqual(instance2LatestCp, recoveryLine.RecoveryMap[instance2]);
            Assert.AreEqual(instance3LatestCp, recoveryLine.RecoveryMap[instance3]);
            Assert.AreEqual(instance4LatestCp, recoveryLine.RecoveryMap[instance4]);
        }

    }
}
