using Autofac;
using BlackSP.Core;
using BlackSP.Core.Endpoints;
using BlackSP.Infrastructure.IoC;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using BlackSP.Serialization.Serializers;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacExtensions
    {

        public static ContainerBuilder UseMessageProcessing(this ContainerBuilder builder)
        {
            //TODO: register logger?
            builder.RegisterType<MessageProcessor>().As<IMessageProcessor>().SingleInstance();
            builder.RegisterType<MessageDeliverer>().As<IMessageDeliverer>().SingleInstance();
            builder.RegisterType<MessageDispatcher>().As<IMessageDispatcher>().SingleInstance();
            builder.RegisterType<MessageReceiver>().As<IMessageReceiver>().SingleInstance();
            builder.RegisterType<MessageSerializer>().As<IMessageSerializer>();
            builder.RegisterType<MessagePartitioner>().As<IMessagePartitioner>();

            builder.RegisterInstance(ArrayPool<byte>.Create()); //register one arraypool for all components to share
            builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share

            builder.RegisterType<InputEndpoint>().As<IInputEndpoint>();
            builder.RegisterType<OutputEndpoint>().As<IOutputEndpoint>();
            builder.RegisterType<ProtobufSerializer>().As<ISerializer>();
            return builder;
        }

        public static ContainerBuilder UseOperatorMiddleware(this ContainerBuilder builder, IHostConfiguration hostConfig)
        {
            _ = hostConfig ?? throw new ArgumentNullException(nameof(hostConfig));

            builder.RegisterType(hostConfig.OperatorShellType)
                   .As(typeof(IOperatorShell), hostConfig.OperatorShellType)
                   .InstancePerLifetimeScope();

            builder.RegisterConcreteClassAsType<IOperator>(hostConfig.OperatorType, true);
            return builder;
        }

        public static ContainerBuilder RegisterConcreteClassAsDefined(this ContainerBuilder builder, Type concreteType, bool asSingleton = false)
        {
            var registration = builder.RegisterType(concreteType).AsImplementedInterfaces().As(concreteType);
            _ = asSingleton ? registration.SingleInstance() : registration.InstancePerDependency();
            return builder;
        }

        public static ContainerBuilder RegisterConcreteClassAsType<T>(this ContainerBuilder builder, Type concreteType, bool asSingleton = false)
        {
            var registration = builder.RegisterType(concreteType).As(typeof(T), concreteType);
            _ = asSingleton ? registration.SingleInstance() : registration.InstancePerDependency();
            return builder;
        }

        public static ContainerBuilder RegisterAllConcreteClassesOfType<T>(this ContainerBuilder builder, string inNamespace = "BlackSP", bool asSingleton = false)
        {
            var concreteTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                .Where(p => p.IsAssignableTo<T>() && !p.IsInterface && !p.IsAbstract && p.IsInNamespace(inNamespace));

            foreach (var concreteType in concreteTypes)
            {
                builder.RegisterConcreteClassAsType<T>(concreteType, asSingleton);
            }
            return builder;
        }

    }
}
