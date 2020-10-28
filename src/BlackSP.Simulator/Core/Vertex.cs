using Autofac;
using BlackSP.Core.Processors;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Models;
using BlackSP.Infrastructure;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Simulator.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Kernel.Models;
using BlackSP.Infrastructure.Layers.Common;

namespace BlackSP.Simulator.Core
{
    /// <summary>
    /// Represents a single machine in the simulated distributed system
    /// </summary>
    public class Vertex
    {
        private readonly ILifetimeScope _parentScope;
        private readonly IdentityTable _identityTable;
        private readonly ConnectionTable _connectionTable;
        
        public Vertex(ILifetimeScope parentScope,
                      IdentityTable identityTable, 
                      ConnectionTable connectionTable)
        {
            _parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
        }

        public async Task StartAs(string instanceName, CancellationToken t)
        {
            _ = instanceName ?? throw new ArgumentNullException(nameof(instanceName));

            IHostConfiguration hostConfig = _identityTable.GetHostConfiguration(instanceName);
            var dependencyScope = _parentScope.BeginLifetimeScope(b => b.ConfigureVertexHost(hostConfig));

            ILogger logger = null;
            ControlMessageProcessor controller = null;
            var threads = new List<Task>();
            try
            {
                logger = dependencyScope.Resolve<ILogger>();
                logger.Debug($"Vertex startup initiated");

                controller = dependencyScope.Resolve<ControlMessageProcessor>();
                threads.Add(Task.Run(() => controller.StartProcess(t)));

                //Note: let the vertex start up before creating endpoints (vertex needs to detect endpoint connection)
                await Task.Delay(5000).ConfigureAwait(false);

                var endpointFactory = dependencyScope.Resolve<EndpointFactory>();
                foreach (var endpointConfig in hostConfig.VertexConfiguration.InputEndpoints)
                {
                    var endpoint = new InputEndpointHost(endpointFactory.ConstructInputEndpoint(endpointConfig, false), _connectionTable, dependencyScope.Resolve<ILogger>());
                    threads.Add(endpoint.Start(instanceName, endpointConfig.LocalEndpointName, t));
                }
                logger.Debug($"Input endpoints created");

                foreach (var endpointConfig in hostConfig.VertexConfiguration.OutputEndpoints)
                {
                    var endpoint = new OutputEndpointHost(endpointFactory.ConstructOutputEndpoint(endpointConfig, false), _connectionTable, dependencyScope.Resolve<ILogger>());
                    threads.Add(endpoint.Start(instanceName, endpointConfig.LocalEndpointName, t));
                }
                logger.Debug($"Output endpoints created");
                logger.Debug($"Vertex startup completed");
                var exitedThread = await Task.WhenAny(threads).ConfigureAwait(false); //double await as whenany returns the task that completed
                await exitedThread.ConfigureAwait(false);
                t.ThrowIfCancellationRequested();
            }
            finally
            {
                logger.Debug($"Vertex has shut down");
                dependencyScope.Dispose();
            }
        }
    }
}
