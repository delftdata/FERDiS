using Autofac;
using Autofac.Core;
using BlackSP.Core;
using BlackSP.Core.Controllers;
using BlackSP.Core.Dispatchers;
using BlackSP.Core.Endpoints;
using BlackSP.Core.MessageSources;
using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Serializers;
using Microsoft.IO;
using System;
using System.Collections.Generic;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacVertexExtensions
    {


        /// <summary>
        /// Configure types to use network receiver as one or more message sources.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="useAsDataSource"></param>
        /// <returns></returns>
        public static ContainerBuilder UseMessageReceiver(this ContainerBuilder builder, bool useAsDataSource = true)
        {
            var exposedTypes = new List<Type>() { typeof(IReceiver), typeof(IMessageSource<ControlMessage>) };
            if (useAsDataSource)
            {
                exposedTypes.Add(typeof(IMessageSource<DataMessage>));
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
            builder.RegisterType<MessageSerializer>().As<IMessageSerializer>();
            builder.RegisterType<MessagePartitioner>().As<IPartitioner>();

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
            builder.RegisterType<MessageSerializer>().As<IMessageSerializer>();
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
            builder.RegisterType<ProtobufSerializer>().As<ISerializer>();
            builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share
            return builder;
        }


        public static ContainerBuilder UseWorkerMonitors(this ContainerBuilder builder)
        {
            builder.RegisterType<ConnectionMonitor>().AsSelf().SingleInstance();
            builder.RegisterType<DataProcessMonitor>().AsSelf().SingleInstance();

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
