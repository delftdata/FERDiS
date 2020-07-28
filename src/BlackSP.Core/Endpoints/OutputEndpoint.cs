using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Streams;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.Endpoints
{

    public class OutputEndpoint : IOutputEndpoint
    {
        public delegate OutputEndpoint Factory(string endpointName);

        private readonly IDispatcher<IMessage> _dispatcher;
        private readonly IEndpointConfiguration _endpointConfiguration;
        private readonly ConnectionMonitor _connectionMonitor;

        public OutputEndpoint(string endpointName, IDispatcher<IMessage> dispatcher, IVertexConfiguration vertexConfiguration, ConnectionMonitor connectionMonitor)
        {
            _ = endpointName ?? throw new ArgumentNullException(nameof(endpointName));
            _ = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));

            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _endpointConfiguration = vertexConfiguration.OutputEndpoints.First(x => x.LocalEndpointName == endpointName);

            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
        }

        /// <summary>
        /// Starts a blocking loop that will check the registered remote shard's output queue for
        /// new events and write them to the provided outputstream.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="remoteShardId"></param>
        /// <param name="t"></param>
        public async Task Egress(Stream outputStream, string remoteEndpointName, int remoteShardId, CancellationToken t)
        {
            var msgBytesBuffer = _dispatcher.GetDispatchQueue(_endpointConfiguration, remoteShardId);
            var writer = new PipeStreamWriter(outputStream, _endpointConfiguration.IsControl);
            
            try
            {
                t.ThrowIfCancellationRequested();
                _connectionMonitor.MarkConnected(_endpointConfiguration, remoteShardId);
                foreach (var message in msgBytesBuffer.GetConsumingEnumerable(t))
                {
                    //endpoint drops messages if dispatcher flags indicate there should not be dispatched
                    var endpointTypeDeliveryFlag = _endpointConfiguration.IsControl ? DispatchFlags.Control : DispatchFlags.Data;
                    if (_dispatcher.GetFlags().HasFlag(endpointTypeDeliveryFlag))
                    {
                        await writer.WriteMessage(message, t).ConfigureAwait(false);
                    }
                }
            } 
            catch(Exception e)
            {
                throw;
            }
            finally
            {
                writer.Dispose();
                _connectionMonitor.MarkDisconnected(_endpointConfiguration, remoteShardId);
            }
            
        }
    }
}
