using BlackSP.Checkpointing.Exceptions;
using BlackSP.Checkpointing.UnitTests.Models;
using BlackSP.Kernel.Checkpointing;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace BlackSP.Checkpointing.UnitTests
{
    public class CheckpointServiceTests
    {
        private ICheckpointService checkpointService;

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
            var objectRegisterMock = new Mock<ObjectRegister>();

            checkpointService = new CheckpointService(objectRegisterMock.Object);

            objectA = new ClassA("test string");
            objectB = new ClassB();

            objectX = new ClassX();
            objectZ = new ClassZ();
        }

        [Test]
        public void Register_SucceedsForValidTypes()
        {
            Assert.IsTrue(checkpointService.Register(objectA));
            Assert.IsTrue(checkpointService.Register(objectB));
        }

        [Test]
        public void Register_GracefullyRejectsNonCheckpointableTypes()
        {
            Assert.IsFalse(checkpointService.Register(objectZ));
        }

        [Test]
        public void Register_ThrowsForFailedPreconditions()
        {
            Assert.Throws<CheckpointingPreconditionException>(() => checkpointService.Register(objectX));
        }

        [Test]
        public void Register_ThrowsForNull()
        {
            Assert.Throws<ArgumentNullException>(() => checkpointService.Register(null));
        }
    }
}