using BlackSP.Checkpointing.Protocols;
using BlackSP.Checkpointing.UnitTests.Models;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using Moq;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.UnitTests.Protocols
{
    public class ChandyLamportProtocolTests
    {

        private Mock<IBlockableSource> blockableSourceMock;
        private Mock<ICheckpointService> checkpointServiceMock;
        private Mock<IVertexConfiguration> vertexConfigMock;

        private readonly string InstanceName = "test-instance-0";


        private ChandyLamportProtocol instance => new ChandyLamportProtocol(blockableSourceMock.Object, checkpointServiceMock.Object, vertexConfigMock.Object, new Mock<ILogger>().Object);

        [SetUp]
        public void SetUp()
        {
            blockableSourceMock = new Mock<IBlockableSource>();
            blockableSourceMock.Setup(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()))
                               .Returns(Task.CompletedTask);
            blockableSourceMock.Setup(source => source.Unblock(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()));
                               //.Returns(); void method cannot return

            checkpointServiceMock = new Mock<ICheckpointService>();
            checkpointServiceMock.Setup(service => service.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()))
                                 .ReturnsAsync(Guid.NewGuid());

            vertexConfigMock = new Mock<IVertexConfiguration>();
            vertexConfigMock.Setup(config => config.InstanceName).Returns(InstanceName);
            vertexConfigMock.Setup(config => config.InstanceNames).Returns(new[] { InstanceName });
            vertexConfigMock.Setup(config => config.ShardId).Returns(0);
        }


        [Test]
        public void NoUpstreams_CanTakeDefaultInput()
        {
            ICollection<IEndpointConfiguration> upstreams = new List<IEndpointConfiguration>();
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);
            Assert.DoesNotThrowAsync(() => instance.ReceiveBarrier(default, default));
        }

        [Test]
        public async Task NoUpstreams_InstantlyCheckpoints_And_DoesNotBlock() {
            ICollection<IEndpointConfiguration> upstreams = new List<IEndpointConfiguration>();
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);
            Assert.IsTrue(await instance.ReceiveBarrier(default, default));
            blockableSourceMock.Verify(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Never); //ensure never blocked
            checkpointServiceMock.Verify(serv => serv.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task OneUpstream_Requires_NonDefaultInput()
        {
            var upstreams = new[]
            {
                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-0",
                    RemoteEndpointName = "remote-fake-endpoint-0",
                    RemoteInstanceNames = new[] { "fake-remote-instance-0" },
                    RemoteVertexName = "remote-vertex-1"
                }
            };
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);
            var testInstance = instance;
            Assert.ThrowsAsync<ArgumentNullException>(() => testInstance.ReceiveBarrier(default, default));
        }

        [Test]
        public async Task OneUpstream_InstantlyCheckpoints_And_DoesNotBlock()
        {
            var upstreams = new[]
            {
                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-0",
                    RemoteEndpointName = "remote-fake-endpoint-0",
                    RemoteInstanceNames = new[] { "fake-remote-instance-0" },
                    RemoteVertexName = "remote-vertex-1"
                }
            };
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);

            var testInstance = instance;
            Assert.IsTrue(await testInstance.ReceiveBarrier(upstreams[0], 0));
            //verify that block and unblock happened during this procedure
            blockableSourceMock.Verify(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Once);
            blockableSourceMock.Verify(source => source.Unblock(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Once);
            checkpointServiceMock.Verify(serv => serv.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }


        [Test]
        public async Task TwoUpstreams_DifferentVertices_FirstBlocks_WithoutCheckpoint()
        {
            var upstreams = new[]
            {
                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-0",
                    RemoteEndpointName = "remote-fake-endpoint-0",
                    RemoteInstanceNames = new[] { "fake-remote-instance-0" },
                    RemoteVertexName = "remote-vertex-1"
                },

                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-1",
                    RemoteEndpointName = "remote-fake-endpoint-1",
                    RemoteInstanceNames = new[] { "fake-remote-instance-1" },
                    RemoteVertexName = "remote-vertex-2"
                }
            };
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);

            var testInstance = instance;
            Assert.IsFalse(await testInstance.ReceiveBarrier(upstreams[0], 0)); //returns false indicating not-forwarding the barrier
            //verify that block and unblock happened during this procedure
            blockableSourceMock.Verify(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Once);
            blockableSourceMock.Verify(source => source.Unblock(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Never);
            checkpointServiceMock.Verify(serv => serv.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

        }

        [Test]
        public async Task TwoUpstreams_DifferentVertices_FirstBlocks_WithoutCheckpoint_SecondBlocks_WithCheckpointAndUnblock()
        {
            var upstreams = new[]
            {
                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-0",
                    RemoteEndpointName = "remote-fake-endpoint-0",
                    RemoteInstanceNames = new[] { "fake-remote-instance-0" },
                    RemoteVertexName = "remote-vertex-1"
                },

                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-1",
                    RemoteEndpointName = "remote-fake-endpoint-1",
                    RemoteInstanceNames = new[] { "fake-remote-instance-1" },
                    RemoteVertexName = "remote-vertex-2"
                }
            };
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);

            var testInstance = instance;
            Assert.IsFalse(await testInstance.ReceiveBarrier(upstreams[0], 0)); //returns false indicating not-forwarding the barrier
            Assert.IsTrue(await testInstance.ReceiveBarrier(upstreams[1], 0)); //returns false indicating not-forwarding the barrier
            //verify that block and unblock happened during this procedure
            blockableSourceMock.Verify(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Exactly(2)); //both were blocked at one point
            blockableSourceMock.Verify(source => source.Unblock(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Exactly(2)); //both were unblocked
            checkpointServiceMock.Verify(serv => serv.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task TwoUpstreams_OneVertex_FirstBlocks_WithoutCheckpoint_SecondBlocks_WithCheckpointAndUnblock()
        {
            var upstreams = new[]
            {
                new TestEndpointConfiguration
                {
                    IsControl = false,
                    IsBackchannel = false,
                    IsPipeline = false,
                    LocalEndpointName = "local-fake-endpoint-0",
                    RemoteEndpointName = "remote-fake-endpoint-0",
                    RemoteInstanceNames = new[] { "fake-remote-instance-0", "fake-remote-instance-1" },
                    RemoteVertexName = "remote-vertex-1"
                }
            };
            vertexConfigMock.Setup(config => config.InputEndpoints).Returns(upstreams);

            var testInstance = instance;
            Assert.IsFalse(await testInstance.ReceiveBarrier(upstreams[0], 0)); //returns false indicating not-forwarding the barrier
            blockableSourceMock.Verify(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Exactly(1)); //both were blocked
            blockableSourceMock.Verify(source => source.Unblock(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Never); //both were unblocked
            checkpointServiceMock.Verify(serv => serv.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);


            Assert.IsTrue(await testInstance.ReceiveBarrier(upstreams[0], 1)); //returns false indicating not-forwarding the barrier
            //verify that block and unblock happened during this procedure
            blockableSourceMock.Verify(source => source.Block(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Exactly(2)); //both were blocked
            blockableSourceMock.Verify(source => source.Unblock(It.IsAny<IEndpointConfiguration>(), It.IsAny<int>()), Times.Exactly(2)); //both were unblocked
            checkpointServiceMock.Verify(serv => serv.TakeCheckpoint(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }
    }
}
