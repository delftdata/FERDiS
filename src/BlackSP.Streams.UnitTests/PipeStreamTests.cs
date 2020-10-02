using Nerdbank.Streams;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BlackSP.Streams.UnitTests
{
    public class PipeStreamTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task CanTransmitBytes()
        {
            var (fromStream, toStream) = FullDuplexStream.CreatePair();

            var pipe = fromStream.UsePipe().Output;
            var writer = new PipeStreamWriter(pipe, true);

            var pipe2 = toStream.UsePipe().Input;
            var reader = new PipeStreamReader(pipe2);

            await writer.WriteMessage(new byte[42], default).ConfigureAwait(false);
            //await writer.FlushAndRefreshBuffer(0, default);
            await writer.WriteMessage(new byte[43], default).ConfigureAwait(false);
            //await writer.WriteMessage(new byte[44], default).ConfigureAwait(false);
            var msg = await reader.ReadNextMessage(default).ConfigureAwait(false);
            Assert.AreEqual(42, msg.Length);
            msg = await reader.ReadNextMessage(default).ConfigureAwait(false);
            Assert.AreEqual(43, msg.Length);
        }
    }
}