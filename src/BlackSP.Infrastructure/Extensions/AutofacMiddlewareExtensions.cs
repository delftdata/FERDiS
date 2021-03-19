using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Infrastructure.Layers.Control.Handlers;
using BlackSP.Infrastructure.Layers.Data.Handlers;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Checkpointing;
using BlackSP.Core.MessageProcessing.Handlers;
using System;

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

        public static ContainerBuilder AddDataLayerMessageHandlersForWorker<TShell, TOperator>(this ContainerBuilder builder, CheckpointCoordinationMode cpMode)
        {
            //pre operator handlers
            builder.RegisterType<MetricLoggingHandler<DataMessage>>().As<IHandler<DataMessage>>();
            builder.RegisterType<CheckpointDependencyTrackingReceptionHandler>().As<IHandler<DataMessage>>();

            switch(cpMode)
            {
                case CheckpointCoordinationMode.Uncoordinated:
                    builder.RegisterType<UncoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                case CheckpointCoordinationMode.Coordinated:
                    builder.RegisterType<CoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                case CheckpointCoordinationMode.CommunicationInduced:
                    //TODO: register handler for cic
                    throw new NotImplementedException("NO CIC HANDLER REGISTRATION IMPLEMENTED");
                    break;
            }

            //operator handler
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<OperatorEventHandler>().As<IHandler<DataMessage>>();

            //post operator handlers
            builder.RegisterType<CheckpointDependencyTrackingDispatchHandler>().As<IHandler<DataMessage>>();


            return builder;
        }

        public static ContainerBuilder AddDataLayerMessageHandlersForSource(this ContainerBuilder builder, CheckpointCoordinationMode cpMode)
        {
            builder.RegisterType<MetricLoggingHandler<DataMessage>>().As<IHandler<DataMessage>>();
            switch (cpMode)
            {
                case CheckpointCoordinationMode.Uncoordinated:
                    builder.RegisterType<UncoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                case CheckpointCoordinationMode.Coordinated:
                    builder.RegisterType<CoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                case CheckpointCoordinationMode.CommunicationInduced:
                    //TODO: register handler for cic
                    break;

            }


            builder.RegisterType<CheckpointDependencyTrackingDispatchHandler>().As<IHandler<DataMessage>>();
            return builder;
        }
    }
}
