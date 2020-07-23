using Autofac;
using Autofac.Core;
using BlackSP.Core;
using BlackSP.Core.Controllers;
using BlackSP.Core.Dispatchers;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Sources;
using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Core.Partitioners;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Checkpointing;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacVertexExtensions
    {
        /// <summary>
        /// TODO: ALLES
        /// </summary>
        /// <param name="context"></param>
        static void OnActivated_CheckpointServiceRegistration(object sender, IActivatedEventArgs<object> context)
        {            
            object obj = context.Instance;
            var manager = context.Context.Resolve<ICheckpointService>();
            var isRegistered = manager.RegisterObject(obj);
            //TODO: log information
        }

        public static ContainerBuilder UseCheckpointingService(this ContainerBuilder builder, bool autoRegisterComponents = false)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            builder.RegisterType<CheckpointDependencyTracker>().AsSelf();
            builder.RegisterType<ObjectRegistry>().AsSelf();
            builder.RegisterType<AzureBackedCheckpointStorage>().AsImplementedInterfaces();
            //above are the dependencies of the service below
            builder.RegisterType<CheckpointService>().As<ICheckpointService>().InstancePerLifetimeScope();
            
            if(autoRegisterComponents)
            {
                //include eventhandlers that ensure any DI resolved object is registered with the CheckpointService
                builder.ComponentRegistryBuilder.Registered += (object sender, ComponentRegisteredEventArgs e) => {
                    e.ComponentRegistration.Activated += OnActivated_CheckpointServiceRegistration;
                };
            }
            
            return builder;
        }

        /// <summary>
        /// Configure types to use network receiver as one or more message sources.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="useAsDataSource"></param>
        /// <returns></returns>
        public static ContainerBuilder UseReceiverMessageSource(this ContainerBuilder builder, bool useAsDataSource = true)
        {
            var exposedTypes = new List<Type>() { typeof(IReceiver), typeof(ISource<ControlMessage>) };
            if (useAsDataSource)
            {
                exposedTypes.Add(typeof(ISource<DataMessage>));
            }
            builder.RegisterType<ReceiverMessageSource>().As(exposedTypes.ToArray()).SingleInstance();
            return builder;
        }

        /// <summary>
        /// Configure types for dispatching messages from a worker instance.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder UseWorkerDispatcher(this ContainerBuilder builder)
        {
            builder.RegisterType<MessageDispatcher>().As<IDispatcher<IMessage>, IDispatcher<ControlMessage>, IDispatcher<DataMessage>>().SingleInstance();
            builder.RegisterType<PooledBufferMessageSerializer>().As<IObjectSerializer<IMessage>>();
            builder.RegisterType<MessageHashPartitioner>().As<IPartitioner<IMessage>>();

            return builder;
        }

        /// <summary>
        /// Configure types for dispatching messages from a coordinator instance
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder UseCoordinatorDispatcher(this ContainerBuilder builder)
        {
            builder.RegisterType<CoordinatorDispatcher>().As<IDispatcher<IMessage>, IDispatcher<ControlMessage>>().SingleInstance();
            builder.RegisterType<PooledBufferMessageSerializer>().As<IObjectSerializer<IMessage>>();
            return builder;
        }

        /// <summary>
        /// Configure streaming input and output endpoint types
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder UseStreamingEndpoints(this ContainerBuilder builder)
        {
            builder.RegisterType<OutputEndpoint>().AsImplementedInterfaces().AsSelf();
            builder.RegisterType<InputEndpoint>().AsImplementedInterfaces().AsSelf();
            return builder;
        }

        /// <summary>
        /// Configure Protobuf-net as internally used serializer.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder UseProtobufSerializer(this ContainerBuilder builder)
        {
            builder.RegisterType<ProtobufStreamSerializer>().As<IStreamSerializer>();
            builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share
            return builder;
        }


        public static ContainerBuilder UseWorkerMonitors(this ContainerBuilder builder)
        {
            builder.RegisterType<ConnectionMonitor>().AsSelf().SingleInstance();
            builder.RegisterType<DataLayerProcessMonitor>().AsSelf().SingleInstance();

            return builder;
        }

        public static ContainerBuilder UseCoordinatorMonitors(this ContainerBuilder builder)
        {
            builder.RegisterType<ConnectionMonitor>().AsSelf().SingleInstance();
            builder.RegisterType<WorkerStateMonitor>().AsSelf().SingleInstance();

            return builder;
        }
    }
}
