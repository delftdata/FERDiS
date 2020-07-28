using Autofac;
using BlackSP.Infrastructure.Builders;
using BlackSP.Infrastructure.Builders.Application;
using BlackSP.Simulator.Builders;
using BlackSP.Simulator.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Simulator
{
    public static class Hosting
    {

        public static IApplicationBuilder CreateDefaultApplicationBuilder()
        {

            var graphBuilder = new SimulatorOperatorVertexGraphBuilder(new ConnectionTable(), new IdentityTable());
            return new ApplicationBuilder(graphBuilder);
        }

    }
}
