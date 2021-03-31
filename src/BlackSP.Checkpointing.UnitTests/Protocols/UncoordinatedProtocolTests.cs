using BlackSP.Checkpointing.Protocols;
using Moq;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.UnitTests.Protocols
{
    class UncoordinatedProtocolTests
    {
        
        private UncoordinatedProtocol GetInstance(TimeSpan interval, DateTime startfrom)
        {
            return new UncoordinatedProtocol(interval, startfrom, new Mock<ILogger>().Object);
        }


        [Test]
        public void CheckpointCondition_OnlyTrue_AtIntervalAndNotEarlier()
        {
            
            var start = DateTime.UtcNow;
            var instance = GetInstance(TimeSpan.FromMilliseconds(100), start);

            Assert.IsFalse(instance.CheckCheckpointCondition(start.AddMilliseconds(-1)));
            Assert.IsFalse(instance.CheckCheckpointCondition(start.AddMilliseconds(50)));
            Assert.IsFalse(instance.CheckCheckpointCondition(start.AddMilliseconds(99)));
            Assert.IsTrue(instance.CheckCheckpointCondition(start.AddMilliseconds(100)));
        }

        [Test]
        public void CheckpointCondition_AgainTrue_AfterSecondInterval()
        {

            var start = DateTime.UtcNow;
            var instance = GetInstance(TimeSpan.FromMilliseconds(100), start);

            Assert.IsFalse(instance.CheckCheckpointCondition(start.AddMilliseconds(1)));
            Assert.IsTrue(instance.CheckCheckpointCondition(start.AddMilliseconds(100)));

            start = start.AddMilliseconds(100);
            instance.SetLastCheckpointUtc(start);
            Assert.IsFalse(instance.CheckCheckpointCondition(start.AddMilliseconds(1)));

            
            Assert.IsFalse(instance.CheckCheckpointCondition(start.AddMilliseconds(99)));
            Assert.IsTrue(instance.CheckCheckpointCondition(start.AddMilliseconds(100)));

        }
    }
}
