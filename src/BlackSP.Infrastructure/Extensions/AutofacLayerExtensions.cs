using Autofac;
using BlackSP.Core.MessageProcessing;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Control.Sources;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacLayerExtensions
    {

        public static ContainerBuilder UseControlLayer(this ContainerBuilder builder)
        {
            //processor
            builder.RegisterType<ControlMessageProcessor>().InstancePerLifetimeScope();
            //pipeline
            builder.RegisterType<MessageHandlerPipeline<ControlMessage>>().As<IPipeline<ControlMessage>>().InstancePerLifetimeScope();
            //message source
            builder.RegisterType<MessageReceiverSource<ControlMessage>>().As<IReceiverSource<ControlMessage>, ISource<ControlMessage>>().AsImplementedInterfaces().InstancePerLifetimeScope();
            
            //checkpoint updates..
            builder.RegisterType<CheckpointTakenSource>().As<ISource<ControlMessage>>();

            //dispatcher (+serializer+partitioner)
            builder.RegisterType<MessagePartitioningDispatcher<ControlMessage>>().As<IDispatcher<ControlMessage>>().SingleInstance();
            builder.RegisterType<PooledBufferMessageSerializer>().As<IObjectSerializer>();
            builder.RegisterType<MessageTargetingPartitioner<ControlMessage>>().AsImplementedInterfaces();

            return builder;
        }

        public static ContainerBuilder UseDataLayer(this ContainerBuilder builder, bool useNetworkAsDataSource = true)
        {
            //processor
            builder.RegisterType<DataMessageProcessor>().InstancePerLifetimeScope();
            //pipeline
            builder.RegisterType<MessageHandlerPipeline<DataMessage>>().As<IPipeline<DataMessage>>().InstancePerLifetimeScope();
            
            if(useNetworkAsDataSource)
            {
                //message source
                builder.RegisterType<MessageReceiverSource<DataMessage>>().As<IReceiverSource<DataMessage>, ISource<DataMessage>>().InstancePerLifetimeScope();
            }
            

            //dispatcher (+serializer+partitioner)
            builder.RegisterType<MessagePartitioningDispatcher<DataMessage>>().As<IDispatcher<DataMessage>>().InstancePerLifetimeScope();
            builder.RegisterType<PooledBufferMessageSerializer>().As<IObjectSerializer>();
            builder.RegisterType<MessageModuloPartitioner<DataMessage>>().AsImplementedInterfaces();

            return builder;
        }
    }
}
