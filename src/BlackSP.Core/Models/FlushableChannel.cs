using BlackSP.Core.Exceptions;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BlackSP.Core.Models
{
    public class FlushableChannel<T> : IFlushable<Channel<T>>
        where T : class
    {

        public Channel<T> UnderlyingCollection { get; private set; }

        private readonly int _capacity;

        private volatile TaskCompletionSource<bool> _tcs;
        
        private bool _isFlushing => _tcs != null;


        public FlushableChannel(int capacity)
        {
            _capacity = capacity;
            InitChannel();
        }

        void InitChannel()
        {
            UnderlyingCollection = Channel.CreateBounded<T>(new BoundedChannelOptions(_capacity) { FullMode = BoundedChannelFullMode.Wait });
        }

        public void ThrowIfFlushingStarted()
        {
            if (_isFlushing)
            {
                throw new FlushInProgressException();
            }
        }

        public async Task BeginFlush()
        {
            Task task;
            if (_tcs == null)
            {
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                task = _tcs.Task;
                UnderlyingCollection.Writer.Complete();
            } 
            else
            {
                task = _tcs.Task;
            }
            await task.ConfigureAwait(false);
        }

        public async Task EndFlush()
        {
            while (_tcs == null)
            {
                await Task.Delay(10).ConfigureAwait(false); //spin until flush started
            }
            _tcs.SetResult(true);
            InitChannel();
            _tcs = null;
        }
    }

    
}
