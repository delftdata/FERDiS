using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Core.Middlewares;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Core.Controllers;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacControlMiddlewareExtensions
    {
        public static ContainerBuilder AddControlMiddlewaresForWorker(this ContainerBuilder builder)
        {
            //TODO: register worker control middlewares in order
            builder.RegisterType<ControlMessageResponseMiddleware>().As<IMiddleware<ControlMessage>>();

            builder.RegisterType<SingleSourceProcessController<DataMessage>>().AsSelf();
            builder.RegisterType<DataProcessControllerMiddleware>().As<IMiddleware<ControlMessage>>();

            return builder;
        }

        public static ContainerBuilder AddControlMiddlewaresForCoordinator(this ContainerBuilder builder)
        {
            //TODO: register coordinator control middlewares in order
            builder.RegisterType<ControlMessageResponseReceptionMiddleware>().AsImplementedInterfaces();

            return builder;
        }
    }
}
