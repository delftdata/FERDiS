using Autofac;
using BlackSP.Infrastructure.Configuration;
using BlackSP.InMemory.Configuration;
using BlackSP.InMemory.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSP.InMemory
{
    public static class Launcher
    {

        /// <summary>
        /// Primary entrypoint for BlackSP.InMemory,
        /// </summary>
        /// <typeparam name="TConfiguration"></typeparam>
        /// <param name="args"></param>
        public static async Task LaunchWithAsync<TConfiguration>(string[] args)
            where TConfiguration : IGraphConfiguration, new()
        {
            
            try
            {
                var userGraphConfiguration = Activator.CreateInstance<TConfiguration>();
                var graphConfigurator = new InMemoryOperatorGraphBuilder(new ConnectionTable(), new IdentityTable());
                userGraphConfiguration.Configure(graphConfigurator); //pass configurator to user defined class
                var container = await graphConfigurator.Build();
                using (var lifetimeScope = container.BeginLifetimeScope())
                {
                    var graph = lifetimeScope.Resolve<VertexGraph>();

                    var vertexThreads = graph.StartAllVertices();
                    var vertexKillThread = Task.Run(() => VertexFaultTrigger(graph));

                    //TODO: console read instance name to kill?
                    await await Task.WhenAny(vertexThreads.Append(vertexKillThread));
                    Console.WriteLine("Graph operation stopped without exception");
                }
            } 
            catch(Exception e)
            {
                Console.WriteLine($"Graph operation stopped with exception:\n{e}");
            }
        }

        private static async Task VertexFaultTrigger(VertexGraph graph)
        {
            while(true)
            {
                var instanceName = Console.ReadLine();
                graph.StopVertex(instanceName);
            }
            
        }
    }
}
