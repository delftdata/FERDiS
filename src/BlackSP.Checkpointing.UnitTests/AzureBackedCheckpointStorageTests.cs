using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Checkpointing.UnitTests.Models;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
using Microsoft.IO;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.UnitTests
{
    class AzureBackedCheckpointStorageTests
    {

        private static readonly string AzureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=vertexstore;AccountKey=3BMGVlrXZq8+NE9caC47KDcpZ8X59vvxFw21NLNNLFhKGgmA8Iq+nr7naEd7YuGGz+M0Xm7dSUhgkUN5N9aMLw==;EndpointSuffix=core.windows.net";

        private ICheckpointStorage checkpointStorage;

        private ICollection<Guid> checkpointIdsToDelete;

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("AZURE_STORAGE_CONN_STRING", AzureStorageConnectionString);

            checkpointStorage = new AzureBackedCheckpointStorage();
            checkpointIdsToDelete = new List<Guid>();
        }

        [Test]
        public async Task CheckpointStoreRetrieve_ReturnsSameCheckpointObject()
        {
            Checkpoint cp = BuildTestCheckpoint();

            //STORE IT
            await checkpointStorage.Store(cp);
            checkpointIdsToDelete.Add(cp.Id);
            //RETRIEVE IT
            var restoredCp = await checkpointStorage.Retrieve(cp.Id);
            //ASSERT SAME
            Assert.AreEqual(cp.Id, restoredCp.Id);
            Assert.IsTrue(cp.Keys.OrderBy(k => k).SequenceEqual(restoredCp.Keys.OrderBy(k => k)));
            Assert.IsTrue(cp.GetDependencies().Keys.OrderBy(k => k).SequenceEqual(restoredCp.GetDependencies().Keys.OrderBy(k => k)));

            foreach (var key in cp.Keys)
            {
                var snapshot1 = cp.GetSnapshot(key);
                var snapshot2 = restoredCp.GetSnapshot(key);
                Assert.AreEqual(snapshot1, snapshot2);
            }
        }

        [Test]
        public async Task CheckpointStoreRetrieve_ReturnsSameCheckpointObject_MultipleCheckpoints()
        {
            Checkpoint cp = BuildTestCheckpoint();
            //STORE IT
            await checkpointStorage.Store(cp);
            checkpointIdsToDelete.Add(cp.Id);


            Checkpoint cp2 = BuildTestCheckpoint();
            //STORE IT
            await checkpointStorage.Store(cp2);
            checkpointIdsToDelete.Add(cp2.Id);


            //RETRIEVE Both
            var restoredCp = await checkpointStorage.Retrieve(cp.Id);
            var restoredCp2 = await checkpointStorage.Retrieve(cp2.Id);

            //ASSERT SAME
            Assert.AreEqual(cp.Id, restoredCp.Id);
            Assert.IsTrue(cp.Keys.OrderBy(k => k).SequenceEqual(restoredCp.Keys.OrderBy(k => k)));
            Assert.IsTrue(cp.GetDependencies().Keys.OrderBy(k => k).SequenceEqual(restoredCp.GetDependencies().Keys.OrderBy(k => k)));

            Assert.AreEqual(cp2.Id, restoredCp2.Id);
            Assert.IsTrue(cp2.Keys.OrderBy(k => k).SequenceEqual(restoredCp2.Keys.OrderBy(k => k)));
            Assert.IsTrue(cp2.GetDependencies().Keys.OrderBy(k => k).SequenceEqual(restoredCp2.GetDependencies().Keys.OrderBy(k => k)));


            foreach (var key in cp.Keys)
            {
                var snapshot1 = cp.GetSnapshot(key);
                var snapshot2 = restoredCp.GetSnapshot(key);
                var snapshot3 = restoredCp2.GetSnapshot(key);
                Assert.AreEqual(snapshot1, snapshot2);
                Assert.AreEqual(snapshot1, snapshot3);

            }
        }

        [TearDown]
        public async Task TearDown()
        {
            foreach(var cpId in checkpointIdsToDelete)
            {
                await checkpointStorage.Delete(cpId);
            }
        }

        private Checkpoint BuildTestCheckpoint()
        {
            //create some objects with an internal state
            var objectA = new ClassA("value");
            objectA.Add(2);
            var objectB = new ClassB();
            objectB.IncrementCounter();
            objectB.SetLargeArraySize(1 << 22); //22 = 32k
            //BUILD A CHECKPOINT
            Guid cpId = Guid.NewGuid();
            var snapshots = new Dictionary<string, ObjectSnapshot>();
            snapshots.Add("objectA", ObjectSnapshot.TakeSnapshot(objectA));
            snapshots.Add("objectB", ObjectSnapshot.TakeSnapshot(objectB));
            var dependencies = new Dictionary<string, Guid>();
            dependencies.Add("vertex1", Guid.NewGuid());
            dependencies.Add("vertex2", Guid.NewGuid());
            dependencies.Add("vertex3", Guid.NewGuid());
            return new Checkpoint(cpId, snapshots, dependencies);
        }

    }
}
