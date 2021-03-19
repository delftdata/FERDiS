using Autofac;
using BlackSP.Core.Dispatchers;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Core.MessageProcessing;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacLayerExtensions
    {

        public static ContainerBuilder UseControlLayer(this ContainerBuilder builder)
        {
            //processor
            builder.RegisterType<ControlMessageProcessor>().InstancePerLifetimeScope();
            //pipeline
            builder.RegisterType<HandlerInvocationPipeline<ControlMessage>>().As<IPipeline<ControlMessage>>().InstancePerLifetimeScope();
            //message source
            builder.RegisterType<ReceiverMessageSource<ControlMessage>>().As<IReceiver<ControlMessage>, ISource<ControlMessage>>().InstancePerLifetimeScope();
            
            //dispatcher
            builder.RegisterType<TargetingMessageDispatcher<ControlMessage>>().As<IDispatcher<ControlMessage>>().SingleInstance();
            builder.RegisterType<PooledBufferMessageSerializer>().As<IObjectSerializer>();

            return builder;
        }

        public static ContainerBuilder UseDataLayer(this ContainerBuilder builder, bool useNetworkAsDataSource = true)
        {
            //processor
            builder.RegisterType<DataMessageProcessor>().InstancePerLifetimeScope();
            //pipeline
            builder.RegisterType<HandlerInvocationPipeline<DataMessage>>().As<IPipeline<DataMessage>>().InstancePerLifetimeScope();
            
            if(useNetworkAsDataSource)
            {
                //message source
                builder.RegisterType<ReceiverMessageSource<DataMessage>>().As<IReceiver<DataMessage>, ISource<DataMessage>>().InstancePerLifetimeScope();
            }
            

            //dispatcher
            builder.RegisterType<PartitioningMessageDispatcher<DataMessage>>().As<IDispatcher<DataMessage>>().InstancePerLifetimeScope();
            builder.RegisterType<PooledBufferMessageSerializer>().As<IObjectSerializer>();
            builder.RegisterType<MessageHashPartitioner>().As<IPartitioner<IMessage>>();

            return builder;
        }
    }
}
