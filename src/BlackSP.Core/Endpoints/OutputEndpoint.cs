using BlackSP.Kernel.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Serialization;
using System.Threading.Tasks;
using BlackSP.Kernel.Operators;
using System.Buffers;
using Microsoft.IO;
using BlackSP.Core.Extensions;
using System.Linq;
using Nerdbank.Streams;
using BlackSP.Kernel;

namespace BlackSP.Core.Endpoints
{

    public class OutputEndpoint : IOutputEndpoint
    {
        private readonly IMessageDispatcher _dispatcher;

        public OutputEndpoint(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Starts a blocking loop that will check the 
        /// registered remote shard's output queue for
        /// new events and write them to the provided
        /// outputstream.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        public async Task Egress(Stream outputStream, string remoteEndpointName, int remoteShardId, CancellationToken t)
        {
            //cancels when launching thread requests cancel or when operator requests cancel
            //TODO: required? 
            //using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(t, _messageProcessor.GetCancellationToken()))
            {
                var token = t; //TODO: ?? linkedTokenSource.Token;
                var msgBytesBuffer = _dispatcher.GetDispatchQueue(remoteEndpointName, remoteShardId);
                await outputStream.WriteMessagesFrom(msgBytesBuffer, token).ConfigureAwait(false);
            }
        }
    }
}
