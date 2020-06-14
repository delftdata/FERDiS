using Autofac;
using BlackSP.Core.Controllers;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.IoC;
using BlackSP.InMemory.Configuration;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    /// <summary>
    /// Represents a single machine in the simulated distributed system
    /// </summary>
    public class Vertex
    {
        private readonly ILifetimeScope _parentScope;
        private readonly IdentityTable _identityTable;
        private readonly CancellationTokenSource _vertexTokenSource;
        
        public Vertex(ILifetimeScope parentScope, IdentityTable identityTable)
        {
            _parentScope = parentScope ?? throw new ArgumentNullException(nameof(parentScope));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
            _vertexTokenSource = new CancellationTokenSource();
        }

        public async Task StartAs(string instanceName)
        {
            _ = instanceName ?? throw new ArgumentNullException(nameof(instanceName));

            IHostConfiguration hostParameter = _identityTable.GetHostConfiguration(instanceName);
            
            var dependencyScope = _parentScope.BeginLifetimeScope(b => {
                b.UseVertexConfiguration(hostParameter);
                //TODO: swap out for dynamic layer configuration method (also in other infra's)
                if(hostParameter.VertexConfiguration.VertexType == Kernel.VertexType.Operator)
                {
                    b.AddOperatorMiddleware(hostParameter);
                }
            });

            ControlProcessController controller = null;
            try
            {
                var threads = new List<Task>();
                controller = dependencyScope.Resolve<ControlProcessController>();

                //TODO: refactor input and output hosts
                //      - add connectiontable to constructor of Vertex
                //      - construct hosts here (new ...host)
                //      -- Use endpoint factory + pass connectionTable 

                foreach (var endpointConfig in hostParameter.VertexConfiguration.InputEndpoints)
                {
                    var endpoint = dependencyScope.Resolve<InputEndpointHost>();
                    threads.Add(endpoint.Start(instanceName, endpointConfig.LocalEndpointName, _vertexTokenSource.Token));
                }

                foreach (var endpointConfig in hostParameter.VertexConfiguration.OutputEndpoints)
                {
                    var endpoint = dependencyScope.Resolve<OutputEndpointHost>();
                    threads.Add(endpoint.Start(instanceName, endpointConfig.LocalEndpointName, _vertexTokenSource.Token));
                }
                
                threads.Add(Task.Run(() => controller.StartProcess()));

                await await Task.WhenAny(threads); //double await as whenany returns the task that completed
                                                   //TODO: consider waiting for everything to end? stop vertex? use cancellation?
            } 
            catch(Exception e)
            {
                Console.WriteLine($"{instanceName} - Exception in Vertex:\n{e}");
                _vertexTokenSource.Cancel();
                if (controller != null)
                {
                    await controller.StopProcess();
                }
                throw;
            } 
            finally
            {
                dependencyScope.Dispose();
            }
        }
    }
}
