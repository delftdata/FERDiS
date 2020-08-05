using BlackSP.Streams.Extensions;
using Nerdbank.Streams;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Streams
{
    public class PipeStreamReader
    {

        private readonly Stream _stream;
        private readonly PipeReader _reader;

        private ReadResult _lastRead;
        private ReadOnlySequence<byte> _buffer;
        private bool _didRead;
        
        public PipeStreamReader(Stream source)
        {
            _stream = source ?? throw new ArgumentNullException(nameof(source));
            _reader = _stream.UsePipeReader();
            _didRead = false;
        }

        public PipeStreamReader(PipeReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _stream = reader.AsStream();
            _didRead = false;
        }

        public async Task<byte[]> ReadNextMessage(CancellationToken t)
        {
            while(!t.IsCancellationRequested)
            {
                t.ThrowIfCancellationRequested();

                if (_didRead && _buffer.TryReadMessage(out var msgbodySequence, out var readPosition))
                {
                    _buffer = _buffer.Slice(readPosition);
                    
                    return msgbodySequence.ToArray();
                }

                if (_didRead)
                {
                    _reader.AdvanceTo(_buffer.Start);
                }
                _lastRead = await _reader.ReadAsync(t);
                _didRead = true;
                _buffer = _lastRead.Buffer;
            }
            t.ThrowIfCancellationRequested(); //if we end up here it's due to cancellation
            throw null;//so the compiler knows we always throw here
        }
    }
}
