using Autofac;
using BlackSP.Core.Operators;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Operators;
using BlackSP.Interfaces.Serialization;
using CRA.ClientLibrary;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.CRA.DI
{
    public class IoC
    {
        private ContainerBuilder _builder;
        private IEnumerable<Type> _typesInRuntime;
        public IoC()
        {
            _builder = new ContainerBuilder();
            _typesInRuntime = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes());
        }

        public IContainer BuildContainer()
        {
            return _builder.Build();
        }

        public IoC RegisterBlackSPComponents()
        {
            RegisterAllConcreteClassesOfType<IOperator>(true);
            RegisterAllConcreteClassesOfType<IInputEndpoint>();
            RegisterAllConcreteClassesOfType<IOutputEndpoint>();
            RegisterAllConcreteClassesOfType<ISerializer>();

            var bspArrayPool = ArrayPool<byte>.Create();
            _builder.RegisterInstance(bspArrayPool); //register one arraypool for all components to share

            var bspMemoryStreamPool = new RecyclableMemoryStreamManager();
            _builder.RegisterInstance(bspMemoryStreamPool); //register one memorystreampool for all components to share

            return this;
        }

        public IoC RegisterCRAComponents()
        {
            //No need to register types of VertexBase, as this gets instantiated by CRA
            RegisterAllConcreteClassesOfType<IAsyncShardedVertexInputEndpoint>();
            RegisterAllConcreteClassesOfType<IAsyncShardedVertexOutputEndpoint>();
            return this;
        }

        private void RegisterAllConcreteClassesOfType<T>(bool asSingleton = false)
        {
            var concreteTypes = _typesInRuntime
                .Where(p => typeof(T).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var concreteType in concreteTypes)
            {
                var registration = _builder.RegisterType(concreteType).As<T>();
                if (asSingleton)
                {
                    registration.SingleInstance();
                }
            }
        }

        public IoC RegisterOperatorConfiguration(IOperatorConfiguration config)
        {
            config = config ?? throw new ArgumentNullException(nameof(config));

            var configType = config.GetType();
            var asTypes = configType.GetInterfaces();

            _builder
                .RegisterInstance(config)
                .As(asTypes)
                .SingleInstance();

            return this;
        }
    }
}
