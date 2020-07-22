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

        [SetUp]
        public void SetUp()
        {
            Environment.SetEnvironmentVariable("AZURE_STORAGE_CONN_STRING", AzureStorageConnectionString);

            var memstreamManager = new RecyclableMemoryStreamManager();
            checkpointStorage = new AzureBackedCheckpointStorage(memstreamManager);
        }

        [Test]
        public async Task CheckpointStoreRetrieve_ReturnsSameCheckpointObject()
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
            Checkpoint cp = new Checkpoint(cpId, snapshots);
            //STORE IT
            await checkpointStorage.Store(cp);
            //RETRIEVE IT
            var restoredCp = await checkpointStorage.Retrieve(cpId);
            //ASSERT SAME
            Assert.AreEqual(cp.Id, restoredCp.Id);
            Assert.IsTrue(cp.Keys.SequenceEqual(restoredCp.Keys));

            foreach(var key in cp.Keys)
            {
                var snapshot1 = cp.GetSnapshot(key);
                var snapshot2 = restoredCp.GetSnapshot(key);
                //Assert.AreEqual(snapshot1, snapshot2);
            }
            //TEST MULTIPLE CHECKPOINTS
        }

        [TearDown]
        public void TearDown()
        {

        }

    }
}
