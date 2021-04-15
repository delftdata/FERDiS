using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Checkpointing.Protocols;
using Moq;
using Serilog;
using System.Linq;

namespace BlackSP.Checkpointing.UnitTests.Protocols
{
    class HMNRProtocolTests
    {

        string[] testInstances;

        [SetUp]
        public void SetUp()
        {
            testInstances = new[] { "instance1", "instance2", "instance3", "instance4", "instance5" };
        }

        /// <summary>
        /// initialises clocks and initial checkpoint
        /// </summary>
        /// <param name="i"></param>
        /// <param name="instances"></param>
        /// <returns></returns>
        private HMNRProtocol InitialiseInstance(int i, string[] instances)
        {
            var instance = new HMNRProtocol(new Mock<ILogger>().Object);

            instance.InitializeClocks(instances[i], instances);
            instance.BeforeCheckpoint();
            //~checkpoint~
            instance.AfterCheckpoint();

            return instance;
        }

        [Test]
        public void Inititalization_YieldsCorrectClockState()
        {
            var instances = testInstances.Take(3).ToArray();
            var instance1 = InitialiseInstance(0, instances);

            var (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.AreEqual(instances.Length, clock.Length);
            Assert.AreEqual(instances.Length, ckpt.Length);
            Assert.AreEqual(instances.Length, taken.Length);

            Assert.AreEqual(1, clock[0]); //update
            Assert.AreEqual(1, ckpt[0]); //update
            Assert.AreEqual(false, taken[0]); //update
        }

        [Test]
        public void Inititalization_YieldsCorrectClockState_ForAnyIndex()
        {
            var instances = testInstances.Take(3).ToArray();
            var instance = InitialiseInstance(2, instances);

            var (clock, ckpt, taken) = instance.GetPiggybackData();
            Assert.AreEqual(instances.Length, clock.Length);
            Assert.AreEqual(instances.Length, ckpt.Length);
            Assert.AreEqual(instances.Length, taken.Length);

            Assert.AreEqual(1, clock[2]); //update
            Assert.AreEqual(1, ckpt[2]); //update
            Assert.AreEqual(false, taken[2]); //update
        }

        [Test]
        public void AcyclicPattern_ForcesNoCheckpoint()
        {
            var instances = testInstances.Take(5).ToArray();
            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);
            var instance4 = InitialiseInstance(3, instances);
            var instance5 = InitialiseInstance(4, instances);

            //1 sends to 2
            instance1.BeforeSend(instance2.InstanceName);
            var (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsFalse(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);
            
            //3 checkpoints
            instance3.BeforeCheckpoint();
            //~checkpoint~
            instance3.AfterCheckpoint();
            
            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //3 sends to 4
            instance3.BeforeSend(instance4.InstanceName);
            (clock, ckpt, taken) = instance3.GetPiggybackData();
            Assert.IsFalse(instance4.CheckCheckpointCondition(instance3.InstanceName, clock, ckpt, taken));
            instance4.BeforeDeliver(instance3.InstanceName, clock, ckpt, taken);

            //5 sends to 4
            instance5.BeforeSend(instance4.InstanceName);
            (clock, ckpt, taken) = instance5.GetPiggybackData();
            Assert.IsFalse(instance4.CheckCheckpointCondition(instance5.InstanceName, clock, ckpt, taken));
            instance4.BeforeDeliver(instance5.InstanceName, clock, ckpt, taken);
        }

        [Test]
        public void ZCycle_Does_Not_Result_In_Checkpoint_Flurry()
        {
            var instances = testInstances.Take(5).ToArray();
            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);

            //1 sends to 2
            instance1.BeforeSend(instance2.InstanceName);
            var (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsFalse(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);

            //3 checkpoints
            instance3.BeforeCheckpoint();
            //~checkpoint~
            instance3.AfterCheckpoint();

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //3 sends to 1
            instance3.BeforeSend(instance1.InstanceName);
            (clock, ckpt, taken) = instance3.GetPiggybackData();
            Assert.IsTrue(instance1.CheckCheckpointCondition(instance3.InstanceName, clock, ckpt, taken));
            //1 checkpoints (has to due to checkpoint condition)
            instance1.BeforeCheckpoint();
            //~checkpoint~
            instance1.AfterCheckpoint();
            instance1.BeforeDeliver(instance3.InstanceName, clock, ckpt, taken);

            //1 sends to 2
            instance1.BeforeSend(instance2.InstanceName);
            (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsFalse(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);
        }


        [Test]
        public void CausalZPath_WithoutCheckpoint_ForcesNoCheckpoint()
        {
            var instances = testInstances.Take(3).ToArray();
            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            var (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //3 sends to 1
            instance3.BeforeSend(instance1.InstanceName);
            (clock, ckpt, taken) = instance3.GetPiggybackData();
            Assert.IsFalse(instance1.CheckCheckpointCondition(instance3.InstanceName, clock, ckpt, taken));
            instance1.BeforeDeliver(instance3.InstanceName, clock, ckpt, taken);

            //1 sends to 2
            instance1.BeforeSend(instance2.InstanceName);
            (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsFalse(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);
        }

        [Test]
        public void CausalZPath__WithCheckpoint_ForcesCheckpoint()
        {
            var instances = testInstances.Take(3).ToArray();
            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            var (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //3 checkpoints
            instance3.BeforeCheckpoint();
            //~checkpoint~
            instance3.AfterCheckpoint();

            //3 sends to 1
            instance3.BeforeSend(instance1.InstanceName);
            (clock, ckpt, taken) = instance3.GetPiggybackData();
            Assert.IsFalse(instance1.CheckCheckpointCondition(instance3.InstanceName, clock, ckpt, taken));
            instance1.BeforeDeliver(instance3.InstanceName, clock, ckpt, taken);

            //1 sends to 2 (message reception would put 3's checkpoint on a z-cycle, making it useless, so protocol should prevent this by forcing a checkpoint)
            instance1.BeforeSend(instance2.InstanceName);
            (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsTrue(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);
        }

        [Test]
        public void NonCausalZPath_DoesNotForcesCheckpoint_WhenNoOtherCheckpointTaken()
        {
            var instances = testInstances.Take(3).ToArray();
            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            var (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //1 sends to 2
            instance1.BeforeSend(instance2.InstanceName);
            (clock, ckpt, taken) = instance1.GetPiggybackData();
            //protocol pre-emptively 
            Assert.IsFalse(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);
        }

        [Test]
        public void NonCausalZPath_ForcesCheckpoint()
        {
            var instances = testInstances.Take(3).ToArray();

            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            var (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //1 checkpoints 
            instance1.BeforeCheckpoint();
            //~checkpoint~
            instance1.AfterCheckpoint();

            //1 sends to 2
            instance1.BeforeSend(instance2.InstanceName);
            (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsTrue(instance2.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance2.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken); 
        }

        [Test]
        public void DAG_Join_DoesNot_ForceCheckpoint_RegardlessOfBeingAheadOrBehind()
        {
            var instances = testInstances.Take(3).ToArray();
            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);

            //1 sends to 3
            instance1.BeforeSend(instance3.InstanceName);
            var (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);

            //1 checkpoints (behind)
            instance1.BeforeCheckpoint();
            //~checkpoint~
            instance1.AfterCheckpoint();

            //2 checkpoints (ahead)
            instance2.BeforeCheckpoint();
            //~checkpoint~
            instance2.AfterCheckpoint();

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);
        }


        [Test]
        public void Arbitrary_Scenario_Without_ZPaths_ForcedNoCheckpoints()
        {
            var instances = testInstances.Take(4).ToArray();

            var instance1 = InitialiseInstance(0, instances);
            var instance2 = InitialiseInstance(1, instances);
            var instance3 = InitialiseInstance(2, instances);
            var instance4 = InitialiseInstance(3, instances);

            //1 sends to 3
            instance1.BeforeSend(instance3.InstanceName);
            var (clock, ckpt, taken) = instance1.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance1.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance1.InstanceName, clock, ckpt, taken);

            //2 checkpoints (ahead)
            instance2.BeforeCheckpoint();
            //~checkpoint~
            instance2.AfterCheckpoint();

            //2 sends to 3
            instance2.BeforeSend(instance3.InstanceName);
            (clock, ckpt, taken) = instance2.GetPiggybackData();
            Assert.IsFalse(instance3.CheckCheckpointCondition(instance2.InstanceName, clock, ckpt, taken));
            instance3.BeforeDeliver(instance2.InstanceName, clock, ckpt, taken);

            //3 sends to 4
            instance3.BeforeSend(instance4.InstanceName);
            (clock, ckpt, taken) = instance3.GetPiggybackData();
            Assert.IsFalse(instance4.CheckCheckpointCondition(instance3.InstanceName, clock, ckpt, taken));
            instance4.BeforeDeliver(instance3.InstanceName, clock, ckpt, taken);

            //4 sends to 1
            instance4.BeforeSend(instance1.InstanceName);
            (clock, ckpt, taken) = instance4.GetPiggybackData();
            Assert.IsFalse(instance1.CheckCheckpointCondition(instance4.InstanceName, clock, ckpt, taken));
            instance1.BeforeDeliver(instance4.InstanceName, clock, ckpt, taken);
        }

    }
}
