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
using BlackSP.Kernel.Configuration;
using BlackSP.Checkpointing.Protocols;
using BlackSP.Core.Coordination;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacMessageHandlerExtensions
    {
        

        public static ContainerBuilder AddControlLayerMessageHandlersForCoordinator(this ContainerBuilder builder)
        {
            builder.RegisterType<WorkerResponseHandler>().AsImplementedInterfaces();
            builder.RegisterType<CheckpointRestoreResponseHandler>().As<IHandler<ControlMessage>>();

            builder.RegisterType<MessageLoggingSequenceManager>().AsSelf().SingleInstance();
            builder.RegisterType<CheckpointTakenHandler>().As<IHandler<ControlMessage>>();

            builder.RegisterType<LogReplayRequestHandler>().As<IHandler<ControlMessage>>();
            return builder;
        }
        
        public static ContainerBuilder AddControlLayerMessageHandlersForWorker(this ContainerBuilder builder)
        {
            builder.RegisterType<CheckpointRestoreRequestHandler>().As<IHandler<ControlMessage>>();

            //important to place before WorkerRequestHandler (to perform replays right before starting the processor)
            builder.RegisterType<LogReplayResponseHandler>().As<IHandler<ControlMessage>>();

            //important control handler: controls the subprocess that processes/generates data messages
            builder.RegisterType<WorkerRequestHandler>().As<IHandler<ControlMessage>>();
            
            builder.RegisterType<LogPruneRequestHandler>().As<IHandler<ControlMessage>>();
            builder.RegisterType<ChandyLamportBarrierInjectionHandler>().As<IHandler<ControlMessage>>();
            return builder;
        }

        public static ContainerBuilder AddDataLayerMessageHandlersForWorker<TShell, TOperator>(this ContainerBuilder builder, CheckpointCoordinationMode cpMode)
        {
            //pre operator handlers
            builder.RegisterType<MetricLoggingHandler<DataMessage>>().As<IHandler<DataMessage>>();
            switch(cpMode)
            {
                case CheckpointCoordinationMode.Coordinated:
                    builder.RegisterType<CheckpointDependencyTrackingReceptionHandler>().As<IHandler<DataMessage>>(); //updates local CP depencencies

                    builder.RegisterType<ChandyLamportProtocol>().AsSelf();
                    builder.RegisterType<CoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                
                case CheckpointCoordinationMode.Uncoordinated:
                    builder.RegisterType<MessageLoggingPreDeliveryHandler>().As<IHandler<DataMessage>>();
                    builder.RegisterType<CheckpointDependencyTrackingReceptionHandler>().As<IHandler<DataMessage>>(); //updates local CP depencencies

                    builder.RegisterType<UncoordinatedProtocol>().AsSelf();
                    builder.RegisterType<UncoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                
                case CheckpointCoordinationMode.CommunicationInduced:
                    builder.RegisterType<MessageLoggingPreDeliveryHandler>().As<IHandler<DataMessage>>();

                    builder.RegisterType<UncoordinatedProtocol>().AsSelf();
                    builder.RegisterType<HMNRProtocol>().AsSelf().SingleInstance();
                    builder.RegisterType<CICPreDeliveryHandler>().As<IHandler<DataMessage>>();
                    
                    builder.RegisterType<CheckpointDependencyTrackingReceptionHandler>().As<IHandler<DataMessage>>(); //updates local CP depencencies
                    break;
            }

            //operator handler
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<OperatorEventHandler>().As<IHandler<DataMessage>>();

            builder.RegisterType<CheckpointDependencyTrackingDispatchHandler>().As<IHandler<DataMessage>>(); //forwards CP dependency on new CP
            switch (cpMode)
            {
                case CheckpointCoordinationMode.Coordinated:
                    break;

                case CheckpointCoordinationMode.Uncoordinated:
                    builder.RegisterType<MessageLoggingPostDeliveryHandler>().As<IHandler<DataMessage>>();
                    break;

                case CheckpointCoordinationMode.CommunicationInduced:
                    builder.RegisterType<CICPostDeliveryHandler>().As<IHandler<DataMessage>>();
                    builder.RegisterType<MessageLoggingPostDeliveryHandler>().As<IHandler<DataMessage>>();
                    break;
            }
            
            return builder;
        }

        public static ContainerBuilder AddDataLayerMessageHandlersForSource(this ContainerBuilder builder, CheckpointCoordinationMode cpMode)
        {
            builder.RegisterType<MetricLoggingHandler<DataMessage>>().As<IHandler<DataMessage>>();
            switch (cpMode)
            {
                case CheckpointCoordinationMode.Uncoordinated:
                    builder.RegisterType<UncoordinatedProtocol>().AsSelf();
                    builder.RegisterType<UncoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    builder.RegisterType<MessageLoggingPostDeliveryHandler>().As<IHandler<DataMessage>>();
                    break;
                case CheckpointCoordinationMode.Coordinated:
                    builder.RegisterType<ChandyLamportProtocol>().AsSelf();
                    builder.RegisterType<CoordinatedCheckpointingHandler>().As<IHandler<DataMessage>>();
                    break;
                case CheckpointCoordinationMode.CommunicationInduced:
                    builder.RegisterType<UncoordinatedProtocol>().AsSelf();
                    builder.RegisterType<HMNRProtocol>().AsSelf().SingleInstance();
                    builder.RegisterType<CICPreDeliveryHandler>().As<IHandler<DataMessage>>();//passive but initialises clocks
                    builder.RegisterType<CICPostDeliveryHandler>().As<IHandler<DataMessage>>();
                    builder.RegisterType<MessageLoggingPostDeliveryHandler>().As<IHandler<DataMessage>>();
                    break;

            }


            builder.RegisterType<CheckpointDependencyTrackingDispatchHandler>().As<IHandler<DataMessage>>();
            return builder;
        }
    }
}
