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
            var userGraphConfiguration = Activator.CreateInstance<TConfiguration>();
            var graphConfigurator = new InMemoryOperatorGraphBuilder(new ConnectionTable());
            userGraphConfiguration.Configure(graphConfigurator); //pass configurator to user defined class
            var graph = await graphConfigurator.BuildGraph();
            //TODO: start operating
        }
    }
}
