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
        public static ReadOnlySequence<byte> ReadMessage(this ReadResult readResult, out ReadOnlySequence<byte> msgBodySequence)
        {
            var buffer = readResult.Buffer;
            var msgLengthSequence = buffer.Slice(0, Math.Min(buffer.Length, 4));
            if (msgLengthSequence.Length != 4)
            {
                msgBodySequence = default;
                //Console.WriteLine("eh");
                return buffer.Slice(0,0); //the reader hasnt received the next message yet, abort to try again
            }

            Span<byte> spanOnStack = stackalloc byte[(int)msgLengthSequence.Length];

            msgLengthSequence.CopyTo(spanOnStack);
            int msgLength = MemoryMarshal.Read<int>(spanOnStack);

            msgBodySequence = buffer.Slice(4, Math.Min(buffer.Length-4, msgLength));
            if (msgBodySequence.Length != msgLength)
            {
                msgBodySequence = default;
                //Console.WriteLine("eh");
                return buffer.Slice(0, 0); //the reader hasnt received the full message yet, abort to try again
            }

            return buffer.Slice(0, msgLength+4);
        }

        /// <summary>
        /// Attempts to read a message from PipeReader's ReadResult</br>
        /// When succesful will also advance the reader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="readResult"></param>
        /// <param name="msgBodySequence"></param>
        /// <returns></returns>
        public static bool TryReadMessage(this PipeReader reader, ReadResult readResult, out ReadOnlySequence<byte> msgBodySequence, out ReadOnlySequence<byte> bufferAfterRead)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            var readSequence = readResult.ReadMessage(out msgBodySequence);

            //slice what was read off the buffer
            bufferAfterRead = readResult.Buffer.Slice(readSequence.End);

            //BUG: buffer gets advanced while result sequence gets queued for processing
            //     once being processed the underlying buffer has been cleared causing exceptions
            //     wait advancing till processing completed?
            //     byte-copy to have data as long as required?
            
            //onProcessed = () => reader.AdvanceTo(buffer.Start, buffer.End);
            return readSequence.Length > 0;
        }
    }
}
