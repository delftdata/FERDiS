using Autofac;
using BlackSP.Infrastructure.IoC;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
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

        public static ContainerBuilder RegisterBlackSPComponents(this ContainerBuilder builder, IHostParameter hostParameter)
        {
            _ = hostParameter ?? throw new ArgumentNullException(nameof(hostParameter));

            builder.RegisterType(hostParameter.OperatorShellType)
                   .As(typeof(IOperatorSocket), hostParameter.OperatorShellType)
                   .InstancePerLifetimeScope();

            builder.RegisterConcreteClassAsDefined(hostParameter.OperatorType, true);
            builder.RegisterConcreteClassAsType<IInputEndpoint>(hostParameter.InputEndpointType);
            builder.RegisterConcreteClassAsType<IOutputEndpoint>(hostParameter.OutputEndpointType);
            builder.RegisterConcreteClassAsType<ISerializer>(hostParameter.SerializerType);

            //TODO: register logger?

            builder.RegisterInstance(ArrayPool<byte>.Create()); //register one arraypool for all components to share
            builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share
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
