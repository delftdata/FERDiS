using BlackSP.Streams.Exceptions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;

namespace BlackSP.Streams.Extensions
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
            var msgLengthSequence = buffer.Slice(buffer.Start, Math.Min(buffer.Length, 4));
            if (msgLengthSequence.Length != 4)
            {
                throw new ReadMessageFromStreamException("Missing message length");
            }

            Span<byte> spanOnStack = stackalloc byte[(int)msgLengthSequence.Length];
            msgLengthSequence.CopyTo(spanOnStack);
            
            int msgLength = MemoryMarshal.Read<int>(spanOnStack);
            if(msgLength == 0)
            {
                throw new ReadMessageFromStreamException("Zero length message");
                //msgBodySequence = default;
                //return buffer.Slice(buffer.Start, 0);
            }

            msgBodySequence = buffer.Slice(4, Math.Min(buffer.Length-4, msgLength));
            if (msgBodySequence.Length != msgLength)
            {
                throw new ReadMessageFromStreamException("Mismatching message length");
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
            bool result;
            try
            {
                var readSequence = buffer.ReadMessage(out msgBodySequence);
                readPosition = readSequence.End;
                //slice what was read off the buffer
                result = true;
            }
            catch(ReadMessageFromStreamException)
            {
                msgBodySequence = buffer.Slice(buffer.Start, 0);
                readPosition = msgBodySequence.End;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Copy bytes in the ReadOnlySequence to a stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static byte[] ToArray(this ReadOnlySequence<byte> buffer)
        {
            Span<byte> msgCopy = stackalloc byte[(int)buffer.Length];
            buffer.CopyTo(msgCopy);
            return msgCopy.ToArray();
        }
    }
}
