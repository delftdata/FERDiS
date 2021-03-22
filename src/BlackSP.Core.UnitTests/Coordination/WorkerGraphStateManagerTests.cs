using BlackSP.Core.Coordination;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
using Moq;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.UnitTests.Coordination
{
    class WorkerGraphStateManagerTests
    {

        WorkerGraphStateManager manager;
        Mock<ICheckpointService> cpServiceMock;
        ICollection<string> instanceNames;

        [SetUp]
        public void SetUp()
        {
            var coordinatorInstanceName = "coordinator";
            instanceNames = new List<string>() { "instance1", "instance2", "instance3", coordinatorInstanceName };
            var loggerMock = new Mock<ILogger>();

            WorkerStateManager.Factory wsmFactory = (name) => new WorkerStateManager(name, loggerMock.Object);
            cpServiceMock = new Mock<ICheckpointService>();
            var rlMock = new Mock<IRecoveryLine>();
            rlMock.Setup(rl => rl.RecoveryMap).Returns(() => new Dictionary<string, Guid>() {
                { "instance1", Guid.NewGuid() },
                { "instance2", Guid.NewGuid() },
                { "instance3", Guid.Empty }, //for these tests we assume one of the three instances is unaffected by the recovery line
            });
            rlMock.Setup(rl => rl.AffectedWorkers).Returns(instanceNames.Take(instanceNames.Count - 2));
            cpServiceMock.Setup(serv => serv.CalculateRecoveryLine(It.IsAny<IEnumerable<string>>())).ReturnsAsync(rlMock.Object);

            var graphConfigMock = new Mock<IVertexGraphConfiguration>();
            graphConfigMock.Setup(gc => gc.InstanceNames).Returns(instanceNames);
            
            var vertexConfigMock = new Mock<IVertexConfiguration>();
            vertexConfigMock.Setup(vc => vc.InstanceName).Returns(coordinatorInstanceName);

            manager = new WorkerGraphStateManager(wsmFactory, cpServiceMock.Object, graphConfigMock.Object, vertexConfigMock.Object, loggerMock.Object);
        }

        [Test]
        public void StartUp_WhenAllWorkersConnect_ShouldSetGraphStateToRunning()
        {
            var workerstateChanges = new Dictionary<string, WorkerState>();
            foreach(var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.OnStateChangeNotificationRequired += (name, state) => workerstateChanges[name] = state;
            }

            Assert.AreEqual(WorkerGraphStateManager.State.Idle, manager.CurrentState); //validate graph starts in idle state
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                Assert.AreEqual(WorkerGraphStateManager.State.Idle, manager.CurrentState); //validate graph remains in the idle state while not all workers have connected
                workerManager.FireTrigger(WorkerStateTrigger.Startup);
            }
            Assert.AreEqual(WorkerGraphStateManager.State.Running, manager.CurrentState);
            
            Assert.AreEqual(instanceNames.Count() - 1, workerstateChanges.Count);
            Assert.IsTrue(workerstateChanges.Values.All(state => state == WorkerState.Running));
        }

        [Test]
        public void Failure_DuringRegularOperation_ShouldSetGraphStateToFaulted()
        {
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.Startup);
            }

            var failingInstance = manager.GetWorkerStateManager("instance1");
            failingInstance.FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);

            var instance2 = manager.GetWorkerStateManager("instance2");
            var instance3 = manager.GetWorkerStateManager("instance3");

            //Assert.AreEqual(instanceNames.Count() - 1, workerstateChanges.Count);

            Assert.AreEqual(WorkerState.Faulted, failingInstance.CurrentState);
            Assert.AreEqual(WorkerState.Halting, instance2.CurrentState);
            Assert.AreEqual(WorkerState.Running, instance3.CurrentState);
        }

        [Test]
        public void Restart_AfterFailure_ShouldSetGraphStateToRecovering()
        {
            var workerstateChanges = new Dictionary<string, WorkerState>();
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.OnStateChangeNotificationRequired += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.Startup);
            }
            var failingWorker = manager.GetAllWorkerStateManagers().First();
            failingWorker.FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            failingWorker.FireTrigger(WorkerStateTrigger.Startup); //make it reconnect

            foreach (var workerManager in manager.GetAllWorkerStateManagers().Where(man => man.CurrentState == WorkerState.Halting))
            {   //complete halting the halting instances
                workerManager.FireTrigger(WorkerStateTrigger.DataProcessorHaltCompleted);
            }

            Assert.AreEqual(WorkerGraphStateManager.State.Restoring, manager.CurrentState);

            Assert.AreEqual("instance1", workerstateChanges.First(kv => kv.Value == WorkerState.Recovering).Key);
            Assert.AreEqual("instance2", workerstateChanges.Skip(1).First(kv => kv.Value == WorkerState.Recovering).Key);
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerState.Running).Key);
        }

        [Test]
        public void RestoreCompletion_DuringRecovery_ShouldSetGraphStateToRunning()
        {
            var workerstateChanges = new Dictionary<string, WorkerState>();
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.OnStateChangeNotificationRequired += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.Startup);
            }
            var failingWorker = manager.GetAllWorkerStateManagers().First();
            failingWorker.FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            failingWorker.FireTrigger(WorkerStateTrigger.Startup); //make it reconnect

            foreach (var workerManager in manager.GetAllWorkerStateManagers().Where(man => man.CurrentState == WorkerState.Halting))
            {   //complete halting the halting instances
                workerManager.FireTrigger(WorkerStateTrigger.DataProcessorHaltCompleted);
            }

            foreach (var recoveringInstanceManager in workerstateChanges.Where(kv => kv.Value == WorkerState.Recovering)
                .Select(kv => manager.GetAllWorkerStateManagers().First(m => m.InstanceName == kv.Key))
                .ToList())
            {
                recoveringInstanceManager.FireTrigger(WorkerStateTrigger.CheckpointRestoreCompleted, recoveringInstanceManager.RestoringCheckpointId);
            }
            Assert.AreEqual(WorkerGraphStateManager.State.Running, manager.CurrentState);
            Assert.IsTrue(workerstateChanges.All(kv => kv.Value == WorkerState.Running));
        }

        [Test]
        public void Failure_AfterAnotherFailure_ShouldSetGraphStateToFaulted()
        {
            var workerstateChanges = new Dictionary<string, WorkerState>();
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.Startup);
            }

            var inst1 = manager.GetWorkerStateManager("instance1");
            var inst2 = manager.GetWorkerStateManager("instance2");
            var inst3 = manager.GetWorkerStateManager("instance3");

            inst1.FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);
            Assert.AreEqual(WorkerState.Faulted, inst1.CurrentState);
            Assert.AreEqual(WorkerState.Halting, inst2.CurrentState);
            Assert.AreEqual(WorkerState.Running, inst3.CurrentState);

            inst2.FireTrigger(WorkerStateTrigger.Failure); //fail the second instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);
            Assert.AreEqual(WorkerState.Faulted, inst1.CurrentState);
            Assert.AreEqual(WorkerState.Faulted, inst2.CurrentState);
            Assert.AreEqual(WorkerState.Running, inst3.CurrentState);
        }

    }
}
