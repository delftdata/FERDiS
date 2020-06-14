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
using BlackSP.Core.Streaming;
using BlackSP.Kernel.Models;

namespace BlackSP.Core.Endpoints
{

    public class OutputEndpoint : IOutputEndpoint
    {
        private readonly IDispatcher<IMessage> _dispatcher;
        private readonly IEndpointConfiguration _endpointConfiguration;

        public OutputEndpoint(IDispatcher<IMessage> dispatcher)
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
            var msgBytesBuffer = _dispatcher.GetDispatchQueue(remoteEndpointName, remoteShardId);
            var writer = new MessageStreamWriter(outputStream);
            foreach(var message in msgBytesBuffer.GetConsumingEnumerable(t))
            {
                var endpointTypeDeliveryFlag = _endpointConfiguration.IsControl ? DispatchFlags.Control : DispatchFlags.Data;
                if (_dispatcher.GetFlags().HasFlag(endpointTypeDeliveryFlag))
                {
                    await writer.WriteMessage(message).ConfigureAwait(false);
                }
            }
        }
    }
}
