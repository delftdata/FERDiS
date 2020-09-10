using BlackSP.Core.Coordination;
using Moq;
using NUnit.Framework;
using Serilog;
using Stateless.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Coordination
{
    class WorkerStateManagerTests
    {


        private string testInstanceName;
        private WorkerStateManager stateMachine;

        [SetUp]
        public void SetUp()
        {
            testInstanceName = "SomeInstanceName";
            var loggerMock = new Mock<ILogger>();
            stateMachine = new WorkerStateManager(testInstanceName, loggerMock.Object);
        }

        [Test]
        public void StartsInOfflineState()
        {
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Offline);
        }

        [Test]
        public void ConnectionAfterStartYieldsHaltedState()
        {
            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Halted);
        }

        [Test]
        public void FailureAfterHaltedYieldsFaulted()
        {
            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            bool stateChangeFired = false;
            stateMachine.OnStateChangeNotificationRequired += (name, state) => stateChangeFired = true;
            stateMachine.FireTrigger(WorkerStateManager.Trigger.Failure);
            Assert.IsFalse(stateChangeFired); //event does not get fired as its only used for when the worker needs to be notified of its new state (failure does not)
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Faulted);
        }

        [Test]
        public void StartAfterHaltedYieldsLaunchedState()
        {
            WorkerStateManager.State eventState = WorkerStateManager.State.Offline;
            stateMachine.OnStateChangeNotificationRequired += (name, state) => eventState = state;
            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateManager.Trigger.DataProcessorStart);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Running);
            Assert.AreEqual(eventState, WorkerStateManager.State.Running);
        }

        [Test]
        public void InvalidTriggerYieldsNoStateChange()
        {
            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateManager.Trigger.DataProcessorStart);
            var preInvalidTriggerState = stateMachine.CurrentState;
            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            Assert.AreEqual(stateMachine.CurrentState, preInvalidTriggerState);
        }

        [Test]
        public void OnlyHaltedStateAcceptsCheckpointTriggers()
        {
            var fakeCpId = Guid.NewGuid();

            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateManager.Trigger.DataProcessorStart);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Running);

            //cant start recovery straight from started state
            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Running);
            
            //halt & trigger restore
            stateMachine.FireTrigger(WorkerStateManager.Trigger.DataProcessorHalt);
            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Recovering);

            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreCompleted, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Halted);
        }

        [Test]
        public void StartingSecondCheckpointRestoreInvalidatesInitialCheckpointId()
        {
            stateMachine.OnStateChangeNotificationRequired += (a, b) => Console.WriteLine($"{a} - {b}");

            stateMachine.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);

            var fakeCpId = Guid.NewGuid();
            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Recovering);

            var fakeCpId2 = Guid.NewGuid();
            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreStart, fakeCpId2);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Recovering);

            //completion of initial checkpoint gets ignored
            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreCompleted, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Recovering);

            //but second checkpoint does yield transition
            stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreCompleted, fakeCpId2);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateManager.State.Halted);
        }

        [Test]
        public void OnlyCheckpointTriggersRequireGuidArgument()
        {
            var fakeCpId = Guid.NewGuid();

            //checkpoint triggers require guid argument
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreStart));
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreCompleted));

            //any other trigger should not include the guid argument (2 examples for test)
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateManager.Trigger.DataProcessorStart, fakeCpId));
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateManager.Trigger.Failure, fakeCpId));
      
        }

    }
}
