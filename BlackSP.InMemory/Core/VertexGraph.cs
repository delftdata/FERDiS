using Autofac;
using BlackSP.InMemory.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class VertexGraph
    {
        private readonly ILifetimeScope _lifetimeScope;
        private readonly IdentityTable _identityTable;

        public VertexGraph(ILifetimeScope lifetimeScope, IdentityTable identityTable)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
        }

        public IEnumerable<Task> StartOperating()
        {
            foreach (var instanceName in _identityTable.GetAllInstanceNames())
            {
                yield return StartVertexWithAutoRestart(instanceName, 0, TimeSpan.FromSeconds(1));
            }
        }

        private async Task StartVertexWithAutoRestart(string instanceName, int maxRestarts, TimeSpan restartTimeout)
        {
            Vertex v = _lifetimeScope.Resolve<Vertex>();
            while(true)
            {
                try
                {
                    await v.StartAs(instanceName);
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
                    Console.WriteLine($"{instanceName} - Vertex exited with exceptions, going to restart.\n{e}");
                    await Task.Delay(restartTimeout);
                }
            }             
        }
    }
}
