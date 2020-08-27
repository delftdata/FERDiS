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
    class WorkerStateMachineTests
    {


        private string testInstanceName;
        private WorkerStateMachine stateMachine;

        [SetUp]
        public void SetUp()
        {
            testInstanceName = "SomeInstanceName";
            var loggerMock = new Mock<ILogger>();
            stateMachine = new WorkerStateMachine(testInstanceName, loggerMock.Object);
        }

        [Test]
        public void StartsInOfflineState()
        {
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Offline);
        }

        [Test]
        public void ConnectionAfterStartYieldsHaltedState()
        {
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Halted);
        }

        [Test]
        public void FailureAfterHaltedYieldsFaulted()
        {
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);
            bool stateChangeFired = false;
            stateMachine.OnStateChangeNotificationRequired += (name, state) => stateChangeFired = true;
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.Failure);
            Assert.IsFalse(stateChangeFired); //event does not get fired as its only used for when the worker needs to be notified of its new state (failure does not)
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Faulted);
        }

        [Test]
        public void StartAfterHaltedYieldsLaunchedState()
        {
            WorkerStateMachine.State eventState = WorkerStateMachine.State.Offline;
            stateMachine.OnStateChangeNotificationRequired += (name, state) => eventState = state;
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.DataProcessorStart);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Running);
            Assert.AreEqual(eventState, WorkerStateMachine.State.Running);
        }

        [Test]
        public void InvalidTriggerYieldsNoStateChange()
        {
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.DataProcessorStart);
            var preInvalidTriggerState = stateMachine.CurrentState;
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);
            Assert.AreEqual(stateMachine.CurrentState, preInvalidTriggerState);
        }

        [Test]
        public void OnlyHaltedStateAcceptsCheckpointTriggers()
        {
            var fakeCpId = Guid.NewGuid();

            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.DataProcessorStart);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Running);

            //cant start recovery straight from started state
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Running);
            
            //halt & trigger restore
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.DataProcessorHalt);
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Recovering);

            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreCompleted, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Halted);
        }

        [Test]
        public void StartingSecondCheckpointRestoreInvalidatesInitialCheckpointId()
        {
            stateMachine.OnStateChangeNotificationRequired += (a, b) => Console.WriteLine($"{a} - {b}");

            stateMachine.FireTrigger(WorkerStateMachine.Trigger.NetworkConnected);

            var fakeCpId = Guid.NewGuid();
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Recovering);

            var fakeCpId2 = Guid.NewGuid();
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreStart, fakeCpId2);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Recovering);

            //completion of initial checkpoint gets ignored
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreCompleted, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Recovering);

            //but second checkpoint does yield transition
            stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreCompleted, fakeCpId2);
            Assert.AreEqual(stateMachine.CurrentState, WorkerStateMachine.State.Halted);
        }

        [Test]
        public void OnlyCheckpointTriggersRequireGuidArgument()
        {
            var fakeCpId = Guid.NewGuid();

            //checkpoint triggers require guid argument
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreStart));
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateMachine.Trigger.CheckpointRestoreCompleted));

            //any other trigger should not include the guid argument (2 examples for test)
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateMachine.Trigger.DataProcessorStart, fakeCpId));
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateMachine.Trigger.Failure, fakeCpId));
      
        }

    }
}
