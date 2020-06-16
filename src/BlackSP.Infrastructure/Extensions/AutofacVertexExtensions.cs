﻿using Autofac;
using BlackSP.Core;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Controllers;
using BlackSP.Core.MessageSources;
using BlackSP.Kernel.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Serializers;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using BlackSP.Core.Dispatchers;
using BlackSP.Infrastructure.Models;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacVertexExtensions
    {

        #region Vertex type configurations
        
        /// <summary>
        /// Configure types to have the process behave like a source operator worker instance.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="hostConfig"></param>
        /// <returns></returns>
        public static ContainerBuilder UseSourceWorkerConfiguration<TShell, TOperator>(this ContainerBuilder builder)
        {
            
            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //collector only as control source
            builder.UseMessageReceiver(false);
            
            //data source (local source operator)
            builder.RegisterType<SourceOperatorDataSource>().As<IMessageSource<DataMessage>>();
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().As<IOperator, ISourceOperator<IEvent>>();

            //control processor
            builder.RegisterType<ControlProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<ControlMessage>>().As<IMessageDeliverer<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<DataProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<DataMessage>>().As<IMessageDeliverer<DataMessage>>().SingleInstance();
            //Note: user is expected to register data middlewares himself

            //control + data dispatcher
            builder.UseWorkerDispatcher();

            return builder;
        }

        /// <summary>
        /// Configure types to have the process behave like an operator worker instance.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="hostConfig"></param>
        /// <returns></returns>
        public static ContainerBuilder UseWorkerConfiguration(this ContainerBuilder builder)
        {
            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();
            
            //control + data collector & expose as source
            builder.UseMessageReceiver();

            //control processor
            builder.RegisterType<ControlProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<ControlMessage>>().As<IMessageDeliverer<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<DataProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<DataMessage>>().As<IMessageDeliverer<DataMessage>>().SingleInstance();
            //Note: user is expected to register data middlewares himself

            //control + data dispatcher
            builder.UseWorkerDispatcher();
            return builder;
        }

        /// <summary>
        /// Configure types to have the process behave like a worker-coordinator instance. (instance that coordinates workers)
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="hostConfig"></param>
        /// <returns></returns>
        public static ContainerBuilder UseCoordinatorConfiguration(this ContainerBuilder builder)
        {

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //sources (control only)
            builder.RegisterType<HeartbeatSource>().As<IMessageSource<ControlMessage>>();
            builder.UseMessageReceiver(false);

            //processor (control only)
            builder.RegisterType<ControlProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<ControlMessage>>().As<IMessageDeliverer<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForCoordinator();

            //dispatcher (control only)
            builder.UseCoordinatorDispatcher();

            return builder;
        }
        #endregion

        #region Subsystems for vertices

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
            builder.RegisterType<MessageDispatcher>().As<IDispatcher<IMessage>>().SingleInstance();
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
            builder.RegisterType<CoordinatorDispatcher>().As<IDispatcher<IMessage>>().SingleInstance();
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
            builder.RegisterType<OutputEndpoint>().As<IOutputEndpoint>();
            builder.RegisterType<InputEndpoint>().As<IInputEndpoint>();
            return builder;
        }

        /// <summary>
        /// Configure Protobuf-net as internally used serializer.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder UseProtobufSerializer(this ContainerBuilder builder)
        {
            //TODO: WIP
            //TODO: VERTEX CONFIGURATION
            // AND  ENDPOINT CONFIGURATION
            builder.RegisterType<ProtobufSerializer>().As<ISerializer>();
            builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share
            return builder;
        }

#endregion
    }
}
