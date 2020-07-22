using BlackSP.Checkpointing.Persistence;
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
            //checkpointStorage.Store()

            //BUILD A CHECKPOINT
            //STORE IT
            //RETRIEVE IT
            //ASSERT SAME


            //TEST MULTIPLE CHECKPOINTS
        }

        [TearDown]
        public void TearDown()
        {

        }

    }
}
