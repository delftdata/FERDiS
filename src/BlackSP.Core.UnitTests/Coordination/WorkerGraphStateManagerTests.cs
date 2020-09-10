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
            instanceNames = new List<string>() { "instance1", "instance2", "instance3" };
            var loggerMock = new Mock<ILogger>();

            WorkerStateManager.Factory wsmFactory = (name) => new WorkerStateManager(name, loggerMock.Object);
            cpServiceMock = new Mock<ICheckpointService>();
            var rlMock = new Mock<IRecoveryLine>();
            rlMock.Setup(rl => rl.RecoveryMap).Returns(() => new Dictionary<string, Guid>() {
                { instanceNames.ElementAt(0), Guid.NewGuid() },
                { instanceNames.ElementAt(1), Guid.NewGuid() },
                { instanceNames.ElementAt(2), Guid.Empty }, //for these tests we assume one of the three instances is unaffected by the recovery line
            });
            rlMock.Setup(rl => rl.AffectedWorkers).Returns(instanceNames.Take(instanceNames.Count - 1));
            cpServiceMock.Setup(serv => serv.CalculateRecoveryLine(It.IsAny<IEnumerable<string>>())).ReturnsAsync(rlMock.Object);

            manager = new WorkerGraphStateManager(instanceNames, wsmFactory, cpServiceMock.Object);

        }

        [Test]
        public void StartUp_WhenAllWorkersConnect_ShouldSetGraphStateToRunning()
        {
            var workerstateChanges = new Dictionary<string, WorkerStateManager.State>();
            foreach(var workerManager in manager.WorkerStateManagers)
            {
                workerManager.OnStateChangeNotificationRequired += (name, state) => workerstateChanges[name] = state;
            }

            Assert.AreEqual(WorkerGraphStateManager.State.Idle, manager.CurrentState); //validate graph starts in idle state
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                Assert.AreEqual(WorkerGraphStateManager.State.Idle, manager.CurrentState); //validate graph remains in the idle state while not all workers have connected
                workerManager.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            }
            Assert.AreEqual(WorkerGraphStateManager.State.Running, manager.CurrentState);
            
            Assert.AreEqual(instanceNames.Count(), workerstateChanges.Count);
            Assert.IsTrue(workerstateChanges.Values.All(state => state == WorkerStateManager.State.Running));
        }

        [Test]
        public void Failure_DuringRegularOperation_ShouldSetGraphStateToFaulted()
        {
            var workerstateChanges = new Dictionary<string, WorkerStateManager.State>();
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            }

            manager.WorkerStateManagers.First().FireTrigger(WorkerStateManager.Trigger.Failure); //fail the first instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);

            Assert.AreEqual(instanceNames.Count(), workerstateChanges.Count);
            
            Assert.AreEqual("instance1", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Faulted).Key);
            Assert.AreEqual("instance2", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Halted).Key);
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Running).Key);
        }

        [Test]
        public void Restart_AfterFailure_ShouldSetGraphStateToRecovering()
        {
            var workerstateChanges = new Dictionary<string, WorkerStateManager.State>();
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            }
            var failingWorker = manager.WorkerStateManagers.First();
            failingWorker.FireTrigger(WorkerStateManager.Trigger.Failure); //fail the first instance
            failingWorker.FireTrigger(WorkerStateManager.Trigger.NetworkConnected); //make it reconnect

            Assert.AreEqual(WorkerGraphStateManager.State.Restoring, manager.CurrentState);

            Assert.AreEqual("instance1", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Recovering).Key);
            Assert.AreEqual("instance2", workerstateChanges.Skip(1).First(kv => kv.Value == WorkerStateManager.State.Recovering).Key);
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Running).Key);
        }

        [Test]
        public void RestoreCompletion_DuringRecovery_ShouldSetGraphStateToRunning()
        {
            var workerstateChanges = new Dictionary<string, WorkerStateManager.State>();
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            }
            var failingWorker = manager.WorkerStateManagers.First();
            failingWorker.FireTrigger(WorkerStateManager.Trigger.Failure); //fail the first instance
            failingWorker.FireTrigger(WorkerStateManager.Trigger.NetworkConnected); //make it reconnect

            foreach(var recoveringInstanceManager in workerstateChanges.Where(kv => kv.Value == WorkerStateManager.State.Recovering)
                .Select(kv => manager.WorkerStateManagers.First(m => m.InstanceName == kv.Key))
                .ToList())
            {
                recoveringInstanceManager.FireTrigger(WorkerStateManager.Trigger.CheckpointRestoreCompleted, recoveringInstanceManager.RestoringCheckpointId);
            }
            Assert.AreEqual(WorkerGraphStateManager.State.Running, manager.CurrentState);
            Assert.IsTrue(workerstateChanges.All(kv => kv.Value == WorkerStateManager.State.Running));
        }

        [Test]
        public void Failure_AfterAnotherFailure_ShouldSetGraphStateToFaulted()
        {
            var workerstateChanges = new Dictionary<string, WorkerStateManager.State>();
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.OnStateChange += (name, state) => workerstateChanges[name] = state;
            }
            foreach (var workerManager in manager.WorkerStateManagers)
            {
                workerManager.FireTrigger(WorkerStateManager.Trigger.NetworkConnected);
            }

            manager.WorkerStateManagers.First().FireTrigger(WorkerStateManager.Trigger.Failure); //fail the first instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);
            Assert.AreEqual("instance1", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Faulted).Key);
            Assert.AreEqual("instance2", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Halted).Key);
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Running).Key);

            manager.WorkerStateManagers.Skip(1).First().FireTrigger(WorkerStateManager.Trigger.Failure); //fail the second instance
            Assert.AreEqual(WorkerGraphStateManager.State.Faulted, manager.CurrentState);
            Assert.IsTrue(Enumerable.SequenceEqual(
                new string[] { "instance1", "instance2" }, 
                workerstateChanges.Where(kv => kv.Value == WorkerStateManager.State.Faulted).Select(kv => kv.Key)
            ));
            Assert.AreEqual("instance3", workerstateChanges.First(kv => kv.Value == WorkerStateManager.State.Running).Key);
        }

    }
}
