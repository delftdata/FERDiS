using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Infrastructure.Layers.Control.Handlers;
using BlackSP.Infrastructure.Layers.Data.Handlers;
using BlackSP.Infrastructure.Layers.Control;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacMiddlewareExtensions
    {
        

        public static ContainerBuilder AddControlLayerMessageHandlersForCoordinator(this ContainerBuilder builder)
        {
            //TODO: register coordinator control middlewares in order
            builder.RegisterType<WorkerResponseHandler>().AsImplementedInterfaces();
            builder.RegisterType<CheckpointRestoreResponseHandler>().As<IHandler<ControlMessage>>();

            return builder;
        }
        
        public static ContainerBuilder AddControlLayerMessageHandlersForWorker(this ContainerBuilder builder)
        {
            builder.RegisterType<CheckpointRestoreRequestHandler>().As<IHandler<ControlMessage>>();
            //important control middleware: controls the subprocess that processes/generates data messages
            builder.RegisterType<WorkerRequestHandler>().As<IHandler<ControlMessage>>();
            builder.RegisterType<DataMessageProcessor>().AsSelf();//dependency of DataProcessControllerMiddleware

            builder.RegisterType<DataLayerBarrierInjectionHandler>().As<IHandler<ControlMessage>>();
            return builder;
        }

        public static ContainerBuilder AddDataLayerMessageHandlersForWorker<TShell, TOperator>(this ContainerBuilder builder)
        {
            //pre operator handlers
            builder.RegisterType<CheckpointDependencyTrackingReceptionHandler>().As<IHandler<DataMessage>>();

            //builder.RegisterType<UncoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
            builder.RegisterType<CoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();

            //operator handler
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<OperatorEventHandler>().As<IHandler<DataMessage>>();

            //post operator handlers
            builder.RegisterType<CheckpointDependencyTrackingDispatchHandler>().As<IHandler<DataMessage>>();


            return builder;
        }

        public static ContainerBuilder AddDataLayerMessageHandlersForSource(this ContainerBuilder builder)
        {
            //builder.RegisterType<UncoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
            builder.RegisterType<CoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();


            builder.RegisterType<CheckpointDependencyTrackingDispatchHandler>().As<IHandler<DataMessage>>();
            return builder;
        }
    }
}
