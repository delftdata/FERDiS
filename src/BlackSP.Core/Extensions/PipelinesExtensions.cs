using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;

namespace BlackSP.Core.Extensions
{
    public static class PipelinesExtensions
    {

        /// <summary>
        /// Attempts to read a message from PipeReader's ReadResult
        /// </summary>
        /// <param name="readResult"></param>
        /// <param name="msgBodySequence"></param>
        /// <returns></returns>
        /// <remarks>Does not advance the PipeReader!</remarks>
        public static ReadOnlySequence<byte> ReadMessage(this ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> msgBodySequence)
        {
            var msgLengthSequence = buffer.Slice(0, Math.Min(buffer.Length, 4));
            if (msgLengthSequence.Length != 4)
            {
                msgBodySequence = default;
                return buffer.Slice(buffer.Start, 0); //the reader hasnt received the next message yet, abort to try again
            }

            Span<byte> spanOnStack = stackalloc byte[(int)msgLengthSequence.Length];

            msgLengthSequence.CopyTo(spanOnStack);
            int msgLength = MemoryMarshal.Read<int>(spanOnStack);

            msgBodySequence = buffer.Slice(4, Math.Min(buffer.Length-4, msgLength));
            if (msgBodySequence.Length != msgLength)
            {
                msgBodySequence = default;
                
                return buffer.Slice(buffer.Start, 0); //the reader hasnt received the full message yet, abort to try again
            }

            return buffer.Slice(0, msgLength+4);
        }

        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="readResult"></param>
        /// <param name="msgBodySequence"></param>
        /// <returns></returns>
        public static bool TryReadMessage(this ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> msgBodySequence, out SequencePosition readPosition)
        {
            var readSequence = buffer.ReadMessage(out msgBodySequence);
            readPosition = readSequence.End;
            //slice what was read off the buffer
            return readSequence.Length > 0;
        }
    }
}
