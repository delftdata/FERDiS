using Autofac;
using BlackSP.Core.Controllers;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Models;
using BlackSP.Infrastructure;
using BlackSP.Simulator.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

            IHostConfiguration hostParameter = _identityTable.GetHostConfiguration(instanceName);
            var dependencyScope = _parentScope.BeginLifetimeScope(b => {
                b.RegisterInstance(hostParameter.VertexConfiguration).AsImplementedInterfaces();
                b.RegisterInstance(hostParameter.GraphConfiguration).AsImplementedInterfaces();
                b.RegisterModule(Activator.CreateInstance(hostParameter.StartupModule) as Module);
            });

            ControlLayerProcessController controller = null;
            var threads = new List<Task>();
            try
            {
                controller = dependencyScope.Resolve<ControlLayerProcessController>();
                var inputFactory = dependencyScope.Resolve<InputEndpoint.Factory>();
                var outputFactory = dependencyScope.Resolve<OutputEndpoint.Factory>();

                foreach (var endpointConfig in hostParameter.VertexConfiguration.InputEndpoints)
                {
                    var endpoint = new InputEndpointHost(inputFactory.Invoke(endpointConfig.LocalEndpointName), _connectionTable);
                    threads.Add(endpoint.Start(instanceName, endpointConfig.LocalEndpointName, t));
                }

                foreach (var endpointConfig in hostParameter.VertexConfiguration.OutputEndpoints)
                {
                    var endpoint = new OutputEndpointHost(outputFactory.Invoke(endpointConfig.LocalEndpointName), _connectionTable);
                    threads.Add(endpoint.Start(instanceName, endpointConfig.LocalEndpointName, t));
                }

                threads.Add(Task.Run(() => controller.StartProcess(t)));
                await await Task.WhenAny(threads); //double await as whenany returns the task that completed
                t.ThrowIfCancellationRequested();
            }
            catch(OperationCanceledException) { throw; } //shhh
            catch(Exception e)
            {
                Console.WriteLine($"{instanceName} - DIED WITH EXCEPTION:\n{e}");
                //await Task.WhenAll(threads);
                throw;
            } 
            finally
            {
                Console.WriteLine($"{instanceName} - DIED FINALLY");
                dependencyScope.Dispose();
                Console.WriteLine("SO YEAH>>><<<....");
            }
        }
    }
}
