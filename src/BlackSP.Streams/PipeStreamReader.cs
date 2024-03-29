﻿using BlackSP.Streams.Extensions;
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
    public class PipeStreamReader : IDisposable
    {

        private readonly Stream _stream;
        private readonly PipeReader _reader;

        public double UnreadBufferFraction => (UnreadBufferSize / (double)TotalBufferSize);
        public long UnreadBufferSize { get; private set; }
        public long TotalBufferSize { get; private set; }

        private ReadResult _lastRead;
        private ReadOnlySequence<byte> _buffer;
        private bool _didRead;
        private bool _disposed;

        private PipeStreamReader()
        {
            _didRead = _disposed = false;
            UnreadBufferSize = TotalBufferSize = 0;
        }

        public PipeStreamReader(Stream source) : this()
        {
            _stream = source ?? throw new ArgumentNullException(nameof(source));
            _reader = _stream.UsePipeReader();
            _didRead = _disposed = false;
        }

        public PipeStreamReader(PipeReader reader) : this()
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _stream = reader.AsStream();
            
        }

        public async Task<byte[]> ReadNextMessage(CancellationToken t)
        {
            try
            {
                while (!t.IsCancellationRequested)
                {
                    t.ThrowIfCancellationRequested();

                    if (_buffer.TryReadMessage(out var msgbodySequence, out var readPosition))
                    {
                        _buffer = _buffer.Slice(readPosition);
                        UnreadBufferSize = _buffer.Length;
                        var bytes = msgbodySequence.ToArray();
                        return bytes;
                    }
                    else
                    {
                        await AdvanceReader(t).ConfigureAwait(false);

                    }
                }

                t.ThrowIfCancellationRequested(); //if we end up here it's due to cancellation
                throw null;//so the compiler doesnt complain
            }
            catch (OperationCanceledException) when (t.IsCancellationRequested)
            {
                _reader.CancelPendingRead();
                _didRead = false;
                throw;
            }
            
            
        }

        public async Task AdvanceReader(CancellationToken t)
        {
            if (_didRead)
            {
                try
                {
                    _reader.AdvanceTo(_buffer.Start, _buffer.End);
                }
                finally { }
                //catch (InvalidOperationException) { }//yeah kinda nasty but shh
                //catch (ArgumentOutOfRangeException) { }//buffer advance went wrong.. again nasty but shh
            }
            _lastRead = await _reader.ReadAsync(t).ConfigureAwait(false);
            _buffer = _lastRead.Buffer;
            UnreadBufferSize = TotalBufferSize = _buffer.Length;
            _didRead = true;
        }

        #region dispose pattern

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //dispose managed state (managed objects)
                    _reader.Complete();
                    //_stream.Dispose();
                }
                _disposed = true;
            }
        }

        // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PipeStreamReader()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
