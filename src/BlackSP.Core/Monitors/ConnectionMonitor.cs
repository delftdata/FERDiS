using BlackSP.Kernel.Configuration;
using Serilog;
using System;
using System.Collections.Generic;

namespace BlackSP.Core.Monitors
{
    public class ConnectionMonitor
    {

        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ICollection<ActiveConnection> _activeConnections;
        private readonly ILogger _logger;
        private readonly object _lockObj;
        
        public ConnectionMonitor(IVertexConfiguration vertexConfiguration, ILogger logger)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeConnections = new List<ActiveConnection>();
            _lockObj = new object();
        }

        public delegate void ConnectionChangeEventHandler(ConnectionMonitor sender, ConnectionMonitorEventArgs e);
        public event ConnectionChangeEventHandler OnConnectionChange;

        //mark connected
        public virtual void MarkConnected(IEndpointConfiguration endpoint, int shardId)
        {
            _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            var activeConnection = BuildActiveConnection(endpoint, shardId);

            _logger.Verbose($"Marking endpoint {endpoint.LocalEndpointName} as connected");

            lock (_lockObj)
            {
                if(_activeConnections.Contains(activeConnection))
                {
                    throw new ArgumentException($"connection {shardId} already marked as connected, signal disconnection first", nameof(endpoint));
                }
                _activeConnections.Add(activeConnection);
                //emit state change event
                OnConnectionChange?.Invoke(this, new ConnectionMonitorEventArgs(_vertexConfiguration, _activeConnections, activeConnection, true));
            }
        }
        
        //mark disconnected
        public virtual void MarkDisconnected(IEndpointConfiguration endpoint, int shardId)
        {
            _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            var activeConnection = BuildActiveConnection(endpoint, shardId);
            
            _logger.Verbose($"Marking endpoint {endpoint.LocalEndpointName} as disconnected");

            lock (_lockObj)
            {
                if (!_activeConnections.Contains(activeConnection))
                {
                    throw new ArgumentException("endpoint not marked as connected, signal connection first", nameof(endpoint));
                }
                _activeConnections.Remove(activeConnection);
                //emit state change event
                OnConnectionChange?.Invoke(this, new ConnectionMonitorEventArgs(_vertexConfiguration, _activeConnections, activeConnection, false));
            }

        }

        private ActiveConnection BuildActiveConnection(IEndpointConfiguration endpoint, int shardId)
        {
            bool isUpstream = _vertexConfiguration.InputEndpoints.Contains(endpoint);

            return new ActiveConnection
            {
                Endpoint = endpoint,
                ShardId = shardId,
                IsUpstream = isUpstream
            };
        }
    }
    
    public class ActiveConnection
    {
        public IEndpointConfiguration Endpoint { get; set; }
        public int ShardId { get; set; }
        public bool IsUpstream { get; set; }

        public bool Equals(ActiveConnection other)
        {
            return other != null && Endpoint.LocalEndpointName == other.Endpoint.LocalEndpointName && ShardId == other.ShardId;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as ActiveConnection);
        }

        public override int GetHashCode()
        {
            return $"{Endpoint.LocalEndpointName}${ShardId}".GetHashCode();
        }
    }
    
    public class ConnectionMonitorEventArgs
    {
        public IEnumerable<ActiveConnection> ActiveConnections { get; }

        public Tuple<ActiveConnection, bool> ChangedConnection { get; }
        
        public ConnectionMonitorEventArgs(IVertexConfiguration vertexConfiguration, IEnumerable<ActiveConnection> activeConnections, ActiveConnection changedConnection, bool changedConnectionStatus)
        {
            _ = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));

            ActiveConnections = activeConnections;
            ChangedConnection = Tuple.Create(changedConnection, changedConnectionStatus);
        }
    }
}
