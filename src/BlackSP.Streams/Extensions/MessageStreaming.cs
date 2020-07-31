using Nerdbank.Streams;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Streams.Extensions
{
    public static class MessageStreaming
    {
        /// <summary>
        /// Continuously reads from stream s and puts messages as byte[]s into provided outputqueue.<br/>
        /// Compatible with WriteMessagesFrom(...) method.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="outputQueue"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static async Task ReadMessagesTo(this Stream s, BlockingCollection<byte[]> outputQueue, CancellationToken t)
        {
            _ = outputQueue ?? throw new ArgumentNullException(nameof(outputQueue));
            
            var reader = s.UsePipeReader(0, null, t);
            ReadResult readRes = await reader.ReadAsync(t);
            var currentBuffer = readRes.Buffer;
            
            while (!t.IsCancellationRequested)
            {
                if (currentBuffer.TryReadMessage(out var msgbodySequence, out var readPosition))
                {
                    outputQueue.Add(msgbodySequence.ToArray());
                    currentBuffer = currentBuffer.Slice(readPosition);
                }
                else
                {
                    reader.AdvanceTo(readPosition);
                    readRes = await reader.ReadAsync(t);
                    currentBuffer = readRes.Buffer;
                }
            }
            t.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Iterates inputQueue and writes all bytes to outputstream.<br/>
        /// Compatible with ReadMessagesTo(...) method.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static async Task WriteMessagesFrom(this Stream outputStream, BlockingCollection<byte[]> inputQueue, CancellationToken t)
        {
            _ = inputQueue ?? throw new ArgumentNullException(nameof(inputQueue));

            var writer = outputStream.UsePipeWriter();
            var writtenBytes = 0;
            var currentWriteBuffer = writer.GetMemory();
            foreach (var nextMsgBuffer in inputQueue.GetConsumingEnumerable(t))
            {
                int nextMsgLength = Convert.ToInt32(nextMsgBuffer.Length);
                Memory<byte> nextMsgLengthBytes = BitConverter.GetBytes(nextMsgLength).AsMemory();
                Memory<byte> nextMsgBodyBytes = nextMsgBuffer.AsMemory().Slice(0, nextMsgLength);

                //reserve at least enough space for the actual message + 4 bytes for the leading size integer
                int bytesToWrite = nextMsgLength + 4;

                if (writtenBytes + bytesToWrite > currentWriteBuffer.Length)
                {   //the writebuffer is about to overflow, flush first
                    writer.Advance(writtenBytes);
                    await writer.FlushAsync();
                    writtenBytes = 0;

                    currentWriteBuffer = writer.GetMemory(bytesToWrite);
                }

                var msgLengthBufferSegment = currentWriteBuffer.Slice(writtenBytes, 4);
                nextMsgLengthBytes.CopyTo(msgLengthBufferSegment);

                var msgBodyBufferSegment = currentWriteBuffer.Slice(writtenBytes + 4, nextMsgLength);
                nextMsgBodyBytes.CopyTo(msgBodyBufferSegment);

                writtenBytes += bytesToWrite;
            }
            //before exiting flush any remaining bytes onto the stream
            await writer.FlushAsync();
            
            t.ThrowIfCancellationRequested();
        }
    }
}
