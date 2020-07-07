using Autofac;
using BlackSP.InMemory.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class VertexGraph
    {
        private readonly ILifetimeScope _lifetimeScope;
        private readonly IdentityTable _identityTable;
        private readonly IDictionary<string, CancellationTokenSource> _vertexCancellationSources;
        public VertexGraph(ILifetimeScope lifetimeScope, IdentityTable identityTable)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));

            _vertexCancellationSources = new Dictionary<string, CancellationTokenSource>();
        }

        public IEnumerable<Task> StartAllVertices()
        {
            foreach (var instanceName in _identityTable.GetAllInstanceNames())
            {
                yield return StartVertexWithAutoRestart(instanceName, 3, TimeSpan.FromSeconds(10));
            }
        }

        public void StopVertex(string instanceName)
        {
            _vertexCancellationSources[instanceName]?.Cancel();
        }

        private async Task StartVertexWithAutoRestart(string instanceName, int maxRestarts, TimeSpan restartTimeout)
        {
            Vertex v = _lifetimeScope.Resolve<Vertex>();
            while(true)
            {
                var ctSource = new CancellationTokenSource();
                _vertexCancellationSources[instanceName] = ctSource;
                try
                {
                    Console.WriteLine($"{instanceName} - Vertex starting up");
                    await v.StartAs(instanceName, ctSource.Token);
                    Console.WriteLine($"{instanceName} - Vertex exited without exceptions");
                    return;
                } 
                catch(Exception e)
                {
                    if(maxRestarts-- == 0)
                    {
                        Console.WriteLine($"{instanceName} - Vertex exited with exceptions, not going to restart: exceeded maxRestarts.");
                        throw;
                    }
                    Console.WriteLine($"{instanceName} - Vertex exited with exceptions, going to restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout);
                }
            }             
        }
    }
}
