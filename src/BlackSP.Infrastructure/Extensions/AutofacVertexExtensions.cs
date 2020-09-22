using Autofac;
using Autofac.Core;
using BlackSP.Core;
using BlackSP.Core.Processors;
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
using BlackSP.Core.Coordination;
using BlackSP.Infrastructure.Layers.Common;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacVertexExtensions
    {

        /// <summary>
        /// Configure streaming input and output endpoint types
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder UseStreamingEndpoints(this ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(OutputEndpoint<>)).AsSelf();
            builder.RegisterGeneric(typeof(InputEndpoint<>)).AsSelf();
            builder.RegisterType<EndpointFactory>().AsSelf();
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


        public static ContainerBuilder UseStatusMonitors(this ContainerBuilder builder)
        {
            builder.RegisterType<ConnectionMonitor>().AsSelf().SingleInstance();
            return builder;
        }


        public static ContainerBuilder UseStateManagers(this ContainerBuilder builder)
        {
            
            builder.RegisterType<WorkerStateManager>().AsSelf().InstancePerDependency();
            builder.RegisterType<WorkerGraphStateManager>().AsSelf().SingleInstance();
            return builder;
        }
    }
}
