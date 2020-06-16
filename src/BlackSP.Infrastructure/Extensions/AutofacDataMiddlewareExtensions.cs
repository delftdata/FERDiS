using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Middlewares;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Infrastructure.Models;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacDataMiddlewareExtensions
    {
        public static ContainerBuilder AddOperatorMiddleware(this ContainerBuilder builder, IHostConfiguration hostConfig)
        {
            _ = hostConfig ?? throw new ArgumentNullException(nameof(hostConfig));

            builder.RegisterType(hostConfig.OperatorShellType).As<IOperatorShell>();
            builder.RegisterType(hostConfig.OperatorType).As<IOperator>();
            builder.RegisterType<OperatorMiddleware>().As<IMiddleware<DataMessage>>();

            return builder;
        }
    }
}
