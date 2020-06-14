using Autofac;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.IoC;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Middlewares;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacControlMiddlewareExtensions
    {
        public static ContainerBuilder AddControlMiddlewaresForWorker(this ContainerBuilder builder)
        {
            //TODO: register worker control middlewares in order
            return builder;
        }

        public static ContainerBuilder AddControlMiddlewaresForCoordinator(this ContainerBuilder builder)
        {
            //TODO: register coordinator control middlewares in order
            return builder;
        }
    }
}
