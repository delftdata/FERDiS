using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacModuleExtensions
    {
        public static ContainerBuilder ConfigureVertexHost(this ContainerBuilder builder, IHostConfiguration hostConfiguration)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
            builder.RegisterInstance(hostConfiguration.VertexConfiguration).AsImplementedInterfaces();
            builder.RegisterInstance(hostConfiguration.GraphConfiguration).AsImplementedInterfaces();
            builder.RegisterModule(Activator.CreateInstance(hostConfiguration.StartupModule, hostConfiguration) as Module);
            return builder;
        }
    }
}
