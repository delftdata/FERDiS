using Autofac;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Simulator.Configuration;
using BlackSP.Simulator.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.Simulator
{
    public static class Launcher
    {

        /// <summary>
        /// Primary entrypoint for BlackSP.Simulator,
        /// </summary>
        /// <typeparam name="TConfiguration"></typeparam>
        /// <param name="args"></param>
        public static async Task LaunchWithAsync<TConfiguration>(string[] args)
            where TConfiguration : IGraphConfigurator, new()
        {
            
            var userGraphConfiguration = Activator.CreateInstance<TConfiguration>();
            var graphConfigurator = new InMemoryOperatorGraphBuilder(new ConnectionTable(), new IdentityTable());
            userGraphConfiguration.Configure(graphConfigurator); //pass configurator to user defined class
            var container = await graphConfigurator.Build();
            using (var lifetimeScope = container.BeginLifetimeScope())
            {
                var graph = lifetimeScope.Resolve<VertexGraph>();

                var vertexThreads = graph.StartAllVertices(3, TimeSpan.FromSeconds(5));
                var allWorkerThreads = vertexThreads.Append(Task.Run(() => VertexFaultTrigger(graph)));
                await Task.WhenAll(allWorkerThreads);
            }
            
        }

        private static void VertexFaultTrigger(VertexGraph graph)
        {
            while(true)
            {
                var input = string.Empty;
                try
                {
                    input = Console.ReadLine();
                    if(string.IsNullOrEmpty(input))
                    {
                        break;
                    } 
                    graph.KillVertex(input);
                } 
                catch(Exception e)
                {
                    Console.WriteLine($"Exception while trying to kill vertex with name: {input}.\n{e}");
                }
            }
            
        }
    }
}
