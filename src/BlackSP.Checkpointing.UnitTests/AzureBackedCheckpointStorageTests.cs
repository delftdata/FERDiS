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
using System.Text;

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
        public void Test()
        {
            //create some objects with an internal state
            var objectA = new ClassA("value");
            objectA.Add(2);
            var objectB = new ClassB();
            objectB.IncrementCounter();

            //BUILD A CHECKPOINT
            Guid cpId = Guid.NewGuid();
            var snapshots = new Dictionary<string, ObjectSnapshot>();
            snapshots.Add("objectA", ObjectSnapshot.TakeSnapshot(objectA));
            snapshots.Add("objectB", ObjectSnapshot.TakeSnapshot(objectB));
            Checkpoint cp = new Checkpoint(cpId, snapshots);
            //STORE IT
            checkpointStorage.Store(cp);
            //RETRIEVE IT
            var restoredCp = checkpointStorage.Retrieve(cpId);
            //ASSERT SAME
            Assert.AreEqual(cp.Id, restoredCp.Id);

            //TEST MULTIPLE CHECKPOINTS
        }

        [TearDown]
        public void TearDown()
        {

        }

    }
}
