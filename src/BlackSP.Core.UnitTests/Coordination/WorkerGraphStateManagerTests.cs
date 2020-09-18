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
                { instanceNames.ElementAt(0), Guid.NewGuid() },
                { instanceNames.ElementAt(1), Guid.NewGuid() },
                { instanceNames.ElementAt(2), Guid.Empty }, //for these tests we assume one of the three instances is unaffected by the recovery line
            });
            rlMock.Setup(rl => rl.AffectedWorkers).Returns(instanceNames.Take(instanceNames.Count - 2));
            cpServiceMock.Setup(serv => serv.CalculateRecoveryLine(It.IsAny<IEnumerable<string>>())).ReturnsAsync(rlMock.Object);

            var graphConfigMock = new Mock<IVertexGraphConfiguration>();
            graphConfigMock.Setup(gc => gc.InstanceNames).Returns(instanceNames);
            var vertexConfigMock = new Mock<IVertexConfiguration>();
            vertexConfigMock.Setup(vc => vc.InstanceName).Returns(coordinatorInstanceName);
            manager = new WorkerGraphStateManager(wsmFactory, cpServiceMock.Object, graphConfigMock.Object, vertexConfigMock.Object);

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
                workerManager.FireTrigger(WorkerStateTrigger.NetworkConnected);
            }
            Assert.AreEqual(WorkerGraphStateManager.State.Running, manager.CurrentState);
            
            Assert.AreEqual(instanceNames.Count() - 1, workerstateChanges.Count);
            Assert.IsTrue(workerstateChanges.Values.All(state => state == WorkerState.Running));
        }

        [Test]
        public void Failure_DuringRegularOperation_ShouldSetGraphStateToFaulted()
        {
            var workerstateChanges = new Dictionary<string, WorkerState>();
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.NetworkConnected);
            }

            manager.GetAllWorkerStateManagers().First().FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);

            Assert.AreEqual(instanceNames.Count() - 1, workerstateChanges.Count);
            
            Assert.AreEqual("instance1", workerstateChanges.First(kv => kv.Value == WorkerState.Faulted).Key);
            Assert.AreEqual("instance2", workerstateChanges.First(kv => kv.Value == WorkerState.Halted).Key);
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerState.Running).Key);
        }

        [Test]
        public void Restart_AfterFailure_ShouldSetGraphStateToRecovering()
        {
            var workerstateChanges = new Dictionary<string, WorkerState>();
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.NetworkConnected);
            }
            var failingWorker = manager.GetAllWorkerStateManagers().First();
            failingWorker.FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            failingWorker.FireTrigger(WorkerStateTrigger.NetworkConnected); //make it reconnect

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
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.GetAllWorkerStateManagers())
            {
                workerManager.FireTrigger(WorkerStateTrigger.NetworkConnected);
            }
            var failingWorker = manager.GetAllWorkerStateManagers().First();
            failingWorker.FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            failingWorker.FireTrigger(WorkerStateTrigger.NetworkConnected); //make it reconnect

            foreach(var recoveringInstanceManager in workerstateChanges.Where(kv => kv.Value == WorkerState.Recovering)
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
                workerManager.FireTrigger(WorkerStateTrigger.NetworkConnected);
            }

            manager.GetAllWorkerStateManagers().First().FireTrigger(WorkerStateTrigger.Failure); //fail the first instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);
            Assert.AreEqual("instance1", workerstateChanges.First(kv => kv.Value == WorkerState.Faulted).Key);
            Assert.AreEqual("instance2", workerstateChanges.First(kv => kv.Value == WorkerState.Halted).Key);
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerState.Running).Key);

            manager.GetAllWorkerStateManagers().Skip(1).First().FireTrigger(WorkerStateTrigger.Failure); //fail the second instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);
            Assert.IsTrue(Enumerable.SequenceEqual(
                new string[] { "instance1", "instance2" }, 
                workerstateChanges.Where(kv => kv.Value == WorkerState.Faulted).Select(kv => kv.Key)
            ));
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerState.Running).Key);
        }

    }
}
