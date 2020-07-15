using BlackSP.Core;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Core.Middlewares
{
    public class WorkerStatusResponseMiddleware : IMiddleware<ControlMessage>
    {
        private readonly IVertexConfiguration _vertexConfig;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly DataLayerProcessMonitor _processMonitor;
        private bool UpstreamFullyConnected;
        private bool DownstreamFullyConnected;

        public WorkerStatusResponseMiddleware(IVertexConfiguration vertexConfig, ConnectionMonitor connectionMonitor, DataLayerProcessMonitor processMonitor)
        {
            _vertexConfig = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));

            _connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChange;
        }

        private void ConnectionMonitor_OnConnectionChange(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            UpstreamFullyConnected = e.UpstreamFullyConnected;
            DownstreamFullyConnected = e.DownstreamFullyConnected;
        }

        public Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if(!message.TryGetPayload<WorkerRequestPayload>(out var payload) || payload.RequestType != WorkerRequestType.Status)
            {
                //forward message
                return Task.FromResult(new List<ControlMessage>() { message }.AsEnumerable());
            }
            //received message with status request payload
            var response = new ControlMessage();
            response.AddPayload(new WorkerStatusPayload()
            {
                OriginInstanceName = _vertexConfig.InstanceName,
                UpstreamFullyConnected = UpstreamFullyConnected,
                DownstreamFullyConnected = DownstreamFullyConnected,
                DataProcessActive = _processMonitor.IsActive
            });
            //forward response
            return Task.FromResult(new List<ControlMessage>() { response }.AsEnumerable());
        }
    }
}
