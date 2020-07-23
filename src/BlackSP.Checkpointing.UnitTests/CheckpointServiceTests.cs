using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Exceptions;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Checkpointing.UnitTests.Models;
using BlackSP.Kernel.Checkpointing;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.UnitTests
{
    public class CheckpointServiceTests
    {
        private ICheckpointService checkpointService;

        private string objectAInitialValue;
        private ClassA objectA;
        private ClassB objectB;

        private ClassX objectX;//checkpointable but not serializable
        private ClassZ objectZ;//not checkpointable

        private IEnumerable<object> Objects { 
            get {
                yield return objectA;
                yield return objectB;

                yield return objectX;
                yield return objectZ;
            }
        }

        [SetUp]
        public void Setup()
        {
            var objectRegisterMock = new Mock<ObjectRegistry>();
            var cpStorage = new VolatileCheckpointStorage();//new Mock<ICheckpointStorage>();
            var tracker = new CheckpointDependencyTracker();
            checkpointService = new CheckpointService(objectRegisterMock.Object, tracker, cpStorage);
            
            objectAInitialValue = "initial value";
            objectA = new ClassA(objectAInitialValue);
            objectB = new ClassB();

            objectX = new ClassX();
            objectZ = new ClassZ();
        }

        [Test]
        public void RegisterObject_SucceedsForValidTypeA()
        {
            Assert.IsTrue(checkpointService.RegisterObject(objectA));
        }

        [Test]
        public void RegisterObject_SucceedsForValidTypeB()
        {
            Assert.IsTrue(checkpointService.RegisterObject(objectB));
        }

        [Test]
        public void RegisterObject_GracefullyRejectsNonCheckpointableTypeZ()
        {
            Assert.IsFalse(checkpointService.RegisterObject(objectZ));
        }

        [Test]
        public void RegisterObject_ThrowsForFailedPreconditions()
        {
            Assert.Throws<CheckpointingPreconditionException>(() => checkpointService.RegisterObject(objectX));
        }

        [Test]
        public void RegisterObject_ThrowsForNull()
        {
            Assert.Throws<ArgumentNullException>(() => checkpointService.RegisterObject(null));
        }

        [Test]
        public async Task TakeCheckpoint_ReturnsCheckpointId()
        {
            checkpointService.RegisterObject(objectA);
            checkpointService.RegisterObject(objectB);

            var cpId = await checkpointService.TakeCheckpoint();
            Assert.Greater(cpId, Guid.Empty);
        }

        [Test]
        public async Task RestoreCheckpoint_RestoresStateFromCheckpoint()
        {
            checkpointService.RegisterObject(objectA);
            checkpointService.RegisterObject(objectB);

            objectA.Add(1);

            //assert initial state
            Assert.IsTrue(objectA.GetValue().Equals(objectAInitialValue));
            Assert.AreEqual(objectA.GetTotal(), 1);
            Assert.AreEqual(objectB.Counter, 0);
            //checkpoint initial state;
            var cpId = await checkpointService.TakeCheckpoint();
            Assert.Greater(cpId, Guid.Empty);

            string appendedText = "nice";
            objectA.Append(appendedText);
            objectA.Add(2);
            objectB.IncrementCounter();
            objectB.IncrementCounter();

            //check that state has changed
            Assert.IsTrue(objectA.GetValue().Equals(objectAInitialValue + appendedText));
            Assert.AreEqual(objectA.GetTotal(), 3);
            Assert.AreEqual(objectB.Counter, 2);
            //restore checkpoint
            await checkpointService.RestoreCheckpoint(cpId);
            //check that state has restored
            Assert.IsTrue(objectA.GetValue().Equals(objectAInitialValue));
            Assert.AreEqual(objectA.GetTotal(), 1);
            Assert.AreEqual(objectB.Counter, 0);
        }

    }
}