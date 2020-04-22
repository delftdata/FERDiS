using Autofac;
using BlackSP.Infrastructure.Configuration;
using BlackSP.InMemory.Configuration;
using BlackSP.InMemory.Core;
using System;
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
                var container = await graphConfigurator.BuildGraph();
                using (var lifetimeScope = container.BeginLifetimeScope())
                {
                    var graph = lifetimeScope.Resolve<VertexGraph>();
                    await await Task.WhenAny(graph.StartOperating());
                    Console.WriteLine("Graph operation stopped without exception");
                }
            } catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
