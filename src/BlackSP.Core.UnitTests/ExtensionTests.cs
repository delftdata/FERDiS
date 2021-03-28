using BlackSP.Kernel.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.UnitTests
{
    public class ExtensionTests
    {

        [Test]
        public void GetAllInstancesDownstreamOf_IgnoresDescendants()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string,string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
            });

            var downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance1", true);
            Assert.AreEqual(new[] { "instance2" }, downstreams);

        }

        [Test]
        public void GetAllInstancesDownstreamOf_IncludesDescendants()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
            });

            var downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance1", false);
            Assert.AreEqual(new[] { "instance2", "instance3", "instance4" }, downstreams);

        }

        [Test]
        public void GetAllInstancesDownstreamOf_HandlesBackchannel()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
                Tuple.Create("instance4","instance3"),

            });

            var downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance1", false);
            Assert.AreEqual(new[] { "instance2", "instance3", "instance4" }, downstreams.ToArray());

        }

        [Test]
        public void GetAllInstancesDownstreamOf_HandlesMultiBackchannel()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance1"),
                Tuple.Create("instance2","instance1"),
                Tuple.Create("instance3","instance4"),
            });

            var downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance1", false);
            Assert.AreEqual(new[] { "instance2", "instance3", "instance4" }, downstreams.ToArray());

        }

        [Test]
        public void GetAllInstancesDownstreamOf_HandlesBigCycle()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
                Tuple.Create("instance4","instance1"),
            });

            var downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance1", false);
            Assert.AreEqual(new[] { "instance2", "instance3", "instance4" }, downstreams.ToArray());

            downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance2", false);
            Assert.AreEqual(new[] { "instance3", "instance4", "instance1" }, downstreams.ToArray());

            downstreams = configMock.Object.GetAllInstancesDownstreamOf("instance3", false);
            Assert.AreEqual(new[] { "instance4", "instance1", "instance2" }, downstreams.ToArray());
        }




        [Test]
        public void GetAllInstancesUpstreamOf_IgnoresDescendants()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
            });

            var downstreams = configMock.Object.GetAllInstancesUpstreamOf("instance4", true);
            Assert.AreEqual(new[] { "instance3" }, downstreams);

        }

        [Test]
        public void GetAllInstancesUpstreamOf_IncludesDescendants()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
            });

            var downstreams = configMock.Object.GetAllInstancesUpstreamOf("instance4", false);
            Assert.AreEqual(new[] { "instance3", "instance2", "instance1" }, downstreams);

        }

        [Test]
        public void GetAllInstancesUpstreamOf_HandlesMultiBackchannel()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance1"),
                Tuple.Create("instance2","instance1"),
                Tuple.Create("instance3","instance4"),
            });

            var downstreams = configMock.Object.GetAllInstancesUpstreamOf("instance4", false);
            Assert.AreEqual(new[] { "instance3", "instance2", "instance1" }, downstreams.ToArray());

        }

        [Test]
        public void GetAllInstancesUpstreamOf_HandlesBigCycle()
        {
            var configMock = new Mock<IVertexGraphConfiguration>();

            configMock.Setup(c => c.InstanceConnections).Returns(new List<Tuple<string, string>>
            {
                Tuple.Create("instance1","instance2"),
                Tuple.Create("instance2","instance3"),
                Tuple.Create("instance3","instance4"),
                Tuple.Create("instance4","instance1"),
            });

            var downstreams = configMock.Object.GetAllInstancesUpstreamOf("instance1", false);
            Assert.AreEqual(new[] { "instance4", "instance3", "instance2" }, downstreams.ToArray());

            downstreams = configMock.Object.GetAllInstancesUpstreamOf("instance2", false);
            Assert.AreEqual(new[] { "instance1", "instance4", "instance3" }, downstreams.ToArray());

            downstreams = configMock.Object.GetAllInstancesUpstreamOf("instance3", false);
            Assert.AreEqual(new[] { "instance2", "instance1", "instance4" }, downstreams.ToArray());
        }
    }
}
