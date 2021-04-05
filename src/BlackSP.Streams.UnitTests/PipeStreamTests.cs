using Nerdbank.Streams;
using NUnit.Framework;
using System;
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

        [Test]
        public async Task CanTransmitPartialMessage()
        {
            var (fromStream, toStream) = FullDuplexStream.CreatePair();

            //var pipe = fromStream.UsePipe().Output;
            //var writer = new PipeStreamWriter(pipe, true);

            var pipe2 = toStream.UsePipe().Input;
            var reader = new PipeStreamReader(pipe2);

            var readTask = reader.ReadNextMessage(default);

            //parallel to reading message write buffer content in parts..
            Task.Run(async () =>
            {
                await Task.Delay(10);
                fromStream.Write(BitConverter.GetBytes(50)); //next msg length = 50 bytes;
                await Task.Delay(10);
                fromStream.Write(new byte[30]);
                await Task.Delay(10);
                fromStream.Write(new byte[20]);

                await Task.Delay(10);
                fromStream.Write(BitConverter.GetBytes(30)); //next msg length = 50 bytes;
                await Task.Delay(10);
                fromStream.Write(new byte[17]);
                await Task.Delay(10);
                fromStream.Write(new byte[13]);
            });

            var msg = await readTask.ConfigureAwait(false);
            Assert.AreEqual(50, msg.Length); 
            msg = await reader.ReadNextMessage(default).ConfigureAwait(false);
            Assert.AreEqual(30, msg.Length);
            //Assert.AreEqual(42, msg.Length);
            //msg = await reader.ReadNextMessage(default).ConfigureAwait(false);
            //Assert.AreEqual(43, msg.Length);
        }


        [Test]
        public async Task DoesNotThrowOnEmptyFlush()
        {
            var (fromStream, toStream) = FullDuplexStream.CreatePair();

            var pipe = fromStream.UsePipe().Output;
            var writer = new PipeStreamWriter(pipe, true);

            Assert.DoesNotThrowAsync(() => writer.FlushAndRefreshBuffer());
            Assert.DoesNotThrowAsync(() => writer.FlushAndRefreshBuffer());
            Assert.DoesNotThrowAsync(() => writer.FlushAndRefreshBuffer());
            Assert.DoesNotThrowAsync(() => writer.FlushAndRefreshBuffer());
        }

    }
}