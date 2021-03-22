using Autofac;
using BlackSP.Core.Coordination;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Monitors;
using BlackSP.Infrastructure.Factories;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization;
using Microsoft.IO;

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
            builder.RegisterGeneric(typeof(FlushableTimeoutOutputEndpoint<>)).AsSelf();
            builder.RegisterGeneric(typeof(FlushableTimeoutInputEndpoint<>)).AsSelf();
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
