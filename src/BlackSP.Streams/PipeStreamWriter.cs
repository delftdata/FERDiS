using Nerdbank.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Streams
{
    public class PipeStreamWriter
    {
        private int writtenBytes;
        private PipeWriter writer;
        private Memory<byte> buffer;
        private bool alwaysFlush;
        public PipeStreamWriter(Stream outputStream, bool flushAfterEveryMessage)
        {
            writtenBytes = 0;
            writer = outputStream.UsePipeWriter();
            buffer = writer.GetMemory();
            alwaysFlush = flushAfterEveryMessage;
        }

        public async Task<int> WriteMessage(byte[] message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            int nextMsgLength = Convert.ToInt32(message.Length);
            Memory<byte> nextMsgLengthBytes = BitConverter.GetBytes(nextMsgLength).AsMemory();
            Memory<byte> nextMsgBodyBytes = message.AsMemory().Slice(0, nextMsgLength);
            //reserve at least enough space for the actual message + 4 bytes for the leading size integer
            int bytesToWrite = nextMsgLength + 4;

            await EnsureBufferCapacity(bytesToWrite).ConfigureAwait(false);

            var msgLengthBufferSegment = buffer.Slice(writtenBytes, 4);
            nextMsgLengthBytes.CopyTo(msgLengthBufferSegment);

            var msgBodyBufferSegment = buffer.Slice(writtenBytes + 4, nextMsgLength);
            nextMsgBodyBytes.CopyTo(msgBodyBufferSegment);

            writtenBytes += bytesToWrite;
            if (alwaysFlush)
            {
                await FlushAndRefreshBuffer();
            }

            return writtenBytes;
        }

        private async Task EnsureBufferCapacity(int bytesToWrite)
        {
            if (writtenBytes + bytesToWrite <= buffer.Length)
            {   //buffer capacity is sufficient
                //last flush happened less than 'interval' ago
                return;
            }

            //the writebuffer is about to overflow, flush first
            //(and/or) the last flush happened too long ago so we flush now
            //few and small messages from control layer may take very long to fill the write buffer, thats why there is an early flush mechanism
            await FlushAndRefreshBuffer(bytesToWrite);
        }

        private async Task FlushAndRefreshBuffer(int bytesToWrite = 4096)
        {
            writer.Advance(writtenBytes);
            await writer.FlushAsync();
            writtenBytes = 0;
            buffer = writer.GetMemory(bytesToWrite);
        }

    }
}
