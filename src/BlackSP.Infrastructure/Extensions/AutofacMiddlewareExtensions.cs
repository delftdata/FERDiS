using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Core.Middlewares;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Core.Processors;
using BlackSP.Core;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacMiddlewareExtensions
    {
        

        public static ContainerBuilder AddControlMiddlewaresForCoordinator(this ContainerBuilder builder)
        {
            //TODO: register coordinator control middlewares in order
            builder.RegisterType<WorkerResponseHandler>().AsImplementedInterfaces();
            builder.RegisterType<CheckpointRestoreResponseHandler>().As<IMiddleware<ControlMessage>>();

            return builder;
        }
        
        public static ContainerBuilder AddControlMiddlewaresForWorker(this ContainerBuilder builder)
        {
            //TODO: register worker control middlewares in order
            builder.RegisterType<WorkerRequestHandler>().As<IMiddleware<ControlMessage>>();
            builder.RegisterType<CheckpointRestoreRequestHandler>().As<IMiddleware<ControlMessage>>();

            //important control middleware: controls the subprocess that processes/generates data messages
            builder.RegisterType<WorkerRequestHandler>().As<IMiddleware<ControlMessage>>();
            builder.RegisterType<DataMessageProcessor>().AsSelf();//dependency of DataProcessControllerMiddleware

            return builder;
        }

        public static ContainerBuilder AddOperatorMiddlewareForWorker<TShell, TOperator>(this ContainerBuilder builder)
        {
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<OperatorMiddleware>().As<IMiddleware<DataMessage>>();

            return builder;
        }
    }
}
