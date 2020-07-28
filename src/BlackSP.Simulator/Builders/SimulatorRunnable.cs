using Autofac;
using BlackSP.Infrastructure.Builders;
using BlackSP.Simulator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Simulator.Builders
{
    public class SimulatorRunnable : IApplication
    {

        private readonly IContainer _container;

        public SimulatorRunnable(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public void Run()
        {
            RunAsync().Wait();
        }

        public async Task RunAsync()
        {
            using (var lifetimeScope = _container.BeginLifetimeScope())
            {
                var graph = lifetimeScope.Resolve<VertexGraph>();

                var vertexThreads = graph.StartAllVertices(3, TimeSpan.FromSeconds(5));
                
                var allWorkerThreads = vertexThreads.Append(Task.Run(() => VertexFaultTrigger(graph)));
                await Task.WhenAll(allWorkerThreads);
            }
        }

        private static void VertexFaultTrigger(VertexGraph graph)
        {
            while (true)
            {
                var input = string.Empty;
                try
                {
                    input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input))
                    {
                        break;
                    }
                    graph.KillVertex(input);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while trying to kill vertex with name: {input}.\n{e}");
                }
            }

        }
    }
}
