using Nerdbank.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Streaming
{
    public class MessageStreamWriter
    {
        private int writtenBytes;
        private PipeWriter writer;
        private Memory<byte> buffer;
        private DateTime lastFlush;
        private TimeSpan maxFlushInterval;
        public MessageStreamWriter(Stream outputStream)
        {
            writtenBytes = 0;
            writer = outputStream.UsePipeWriter();
            buffer = writer.GetMemory();
            lastFlush = DateTime.Now;
            maxFlushInterval = TimeSpan.FromSeconds(1);
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

            return writtenBytes += bytesToWrite;
        }

        private async Task EnsureBufferCapacity(int bytesToWrite)
        {
            if (writtenBytes + bytesToWrite <= buffer.Length && (DateTime.Now - lastFlush) < maxFlushInterval)
            {   //buffer capacity is sufficient
                //last flush happened less than 'interval' ago
                return;
            }

            //the writebuffer is about to overflow, flush first
            writer.Advance(writtenBytes);
            await writer.FlushAsync();
            writtenBytes = 0;
            lastFlush = DateTime.Now;
            buffer = writer.GetMemory(bytesToWrite);
        }

    }
}
