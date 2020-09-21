using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Core.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Core.Processors;
using BlackSP.Core;
using BlackSP.Infrastructure.Operators;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacMiddlewareExtensions
    {
        

        public static ContainerBuilder AddControlMiddlewaresForCoordinator(this ContainerBuilder builder)
        {
            //TODO: register coordinator control middlewares in order
            builder.RegisterType<WorkerResponseHandler>().AsImplementedInterfaces();
            builder.RegisterType<CheckpointRestoreResponseHandler>().As<IHandler<ControlMessage>>();

            return builder;
        }
        
        public static ContainerBuilder AddControlMiddlewaresForWorker(this ContainerBuilder builder)
        {
            //TODO: register worker control middlewares in order
            builder.RegisterType<WorkerRequestHandler>().As<IHandler<ControlMessage>>();
            builder.RegisterType<CheckpointRestoreRequestHandler>().As<IHandler<ControlMessage>>();

            //important control middleware: controls the subprocess that processes/generates data messages
            builder.RegisterType<WorkerRequestHandler>().As<IHandler<ControlMessage>>();
            builder.RegisterType<DataMessageProcessor>().AsSelf();//dependency of DataProcessControllerMiddleware

            return builder;
        }

        public static ContainerBuilder AddOperatorMiddlewareForWorker<TShell, TOperator>(this ContainerBuilder builder)
        {
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<OperatorEventHandler>().As<IHandler<DataMessage>>();

            return builder;
        }
    }
}
