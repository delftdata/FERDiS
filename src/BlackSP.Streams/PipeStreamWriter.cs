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
    public class PipeStreamWriter : IDisposable
    {
        private int writtenBytes;
        private Stream stream;
        private PipeWriter writer;
        private Memory<byte> buffer;
        private bool alwaysFlush;
        private bool disposed;

        public PipeStreamWriter(Stream outputStream, bool flushAfterEveryMessage)
        {
            writtenBytes = 0;
            stream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            writer = outputStream.UsePipe().Output;
            buffer = writer.GetMemory();
            alwaysFlush = flushAfterEveryMessage;
            if (!stream.CanWrite)
            {
                throw new IOException($"{this.GetType()} tried to construct with unwritable stream");
            }
            disposed = false;
        }

        public PipeStreamWriter(PipeWriter pipeWriter, bool flushAfterEveryMessage)
        {
            writtenBytes = 0;
            writer = pipeWriter ?? throw new ArgumentNullException(nameof(pipeWriter));
            stream = pipeWriter.AsStream();
            buffer = writer.GetMemory();
            alwaysFlush = flushAfterEveryMessage;

            disposed = false;
        }

        public async Task<int> WriteMessage(byte[] message, CancellationToken t)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            t.ThrowIfCancellationRequested();

            int nextMsgLength = Convert.ToInt32(message.Length);
            Memory<byte> nextMsgLengthBytes = BitConverter.GetBytes(nextMsgLength).AsMemory();
            Memory<byte> nextMsgBodyBytes = message.AsMemory().Slice(0, nextMsgLength);
            //reserve at least enough space for the actual message + 4 bytes for the leading size integer
            int bytesToWrite = nextMsgLength + 4;

            await EnsureBufferCapacity(bytesToWrite, t).ConfigureAwait(false);

            var msgLengthBufferSegment = buffer.Slice(writtenBytes, 4);
            nextMsgLengthBytes.CopyTo(msgLengthBufferSegment);

            var msgBodyBufferSegment = buffer.Slice(writtenBytes + 4, nextMsgLength);
            nextMsgBodyBytes.CopyTo(msgBodyBufferSegment);

            writtenBytes += bytesToWrite;
            if (alwaysFlush)
            {
                await FlushAndRefreshBuffer(t: t).ConfigureAwait(false);
            }

            return writtenBytes;
        }

        public async Task FlushAndRefreshBuffer(int newBufferSize = 4096, CancellationToken t = default)
        {
            writer.Advance(writtenBytes);
            await writer.FlushAsync(t);
            writtenBytes = 0;
            buffer = writer.GetMemory(newBufferSize);
        }

        private async Task EnsureBufferCapacity(int bytesToWrite, CancellationToken t)
        {
            if (writtenBytes + bytesToWrite <= buffer.Length)
            {   //buffer capacity is sufficient
                return;
            }
            //the writebuffer is about to overflow, flush first
            await FlushAndRefreshBuffer(bytesToWrite, t).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    writer.Complete();
                    //stream.Dispose();
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
