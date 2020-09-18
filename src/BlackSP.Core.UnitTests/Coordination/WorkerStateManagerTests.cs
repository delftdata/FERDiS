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
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Offline);
        }

        [Test]
        public void ConnectionAfterStartYieldsHaltedState()
        {
            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Halted);
        }

        [Test]
        public void FailureAfterHaltedYieldsFaulted()
        {
            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);
            bool stateChangeFired = false;
            stateMachine.OnStateChangeNotificationRequired += (name, state) => stateChangeFired = true;
            stateMachine.FireTrigger(WorkerStateTrigger.Failure);
            Assert.IsFalse(stateChangeFired); //event does not get fired as its only used for when the worker needs to be notified of its new state (failure does not)
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Faulted);
        }

        [Test]
        public void StartAfterHaltedYieldsLaunchedState()
        {
            WorkerState eventState = WorkerState.Offline;
            stateMachine.OnStateChangeNotificationRequired += (name, state) => eventState = state;
            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateTrigger.DataProcessorStart);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Running);
            Assert.AreEqual(eventState, WorkerState.Running);
        }

        [Test]
        public void InvalidTriggerYieldsNoStateChange()
        {
            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateTrigger.DataProcessorStart);
            var preInvalidTriggerState = stateMachine.CurrentState;
            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);
            Assert.AreEqual(stateMachine.CurrentState, preInvalidTriggerState);
        }

        [Test]
        public void OnlyHaltedStateAcceptsCheckpointTriggers()
        {
            var fakeCpId = Guid.NewGuid();

            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);
            stateMachine.FireTrigger(WorkerStateTrigger.DataProcessorStart);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Running);

            //cant start recovery straight from started state
            Assert.Throws<InvalidOperationException>(() => stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreStart, fakeCpId));
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Running);
            
            //halt & trigger restore
            stateMachine.FireTrigger(WorkerStateTrigger.DataProcessorHalt);
            stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Recovering);

            stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Halted);
        }

        [Test]
        public void StartingSecondCheckpointRestoreInvalidatesInitialCheckpointId()
        {
            stateMachine.OnStateChangeNotificationRequired += (a, b) => Console.WriteLine($"{a} - {b}");

            stateMachine.FireTrigger(WorkerStateTrigger.NetworkConnected);

            var fakeCpId = Guid.NewGuid();
            stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreStart, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Recovering);

            var fakeCpId2 = Guid.NewGuid();
            stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreStart, fakeCpId2);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Recovering);

            //completion of initial checkpoint gets ignored
            stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted, fakeCpId);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Recovering);

            //but second checkpoint does yield transition
            stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted, fakeCpId2);
            Assert.AreEqual(stateMachine.CurrentState, WorkerState.Halted);
        }

        [Test]
        public void OnlyCheckpointTriggersRequireGuidArgument()
        {
            var fakeCpId = Guid.NewGuid();

            //checkpoint triggers require guid argument
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreStart));
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted));

            //any other trigger should not include the guid argument (2 examples for test)
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateTrigger.DataProcessorStart, fakeCpId));
            Assert.Throws<ArgumentException>(() => stateMachine.FireTrigger(WorkerStateTrigger.Failure, fakeCpId));
      
        }

    }
}
