using Autofac;
using BlackSP.Core.OperatorSockets;
using BlackSP.CRA.Endpoints;
using BlackSP.CRA.Vertices;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using CRA.ClientLibrary;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BlackSP.CRA.DI
{
    public class IoC
    {
        private IHostParameter _options;
        private ContainerBuilder _builder;
        private IEnumerable<Type> _typesInRuntime;
        public IoC(IHostParameter options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            LoadAllAvailableAssemblies();

            _builder = new ContainerBuilder();
            _typesInRuntime = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes());
        }

        public IContainer BuildContainer()
        {

            return _builder.Build();
        }

        public IContainer BuildContainerValidated()
        {
            var container = BuildContainer();
            
            //TODO: validate presence of all required types and throw exception if missing
            container.IsRegistered<IOperatorSocket>();
            
            return container;
        }

        public IoC RegisterBlackSPComponents()
        {
            RegisterConcreteClassAsType<IOperatorSocket>(_options.OperatorType, true);
            RegisterConcreteClassAsDefined(_options.OperatorConfiguration, true);

            RegisterConcreteClassAsType<IInputEndpoint>(_options.InputEndpointType);
            RegisterConcreteClassAsType<IOutputEndpoint>(_options.OutputEndpointType);
            RegisterConcreteClassAsType<ISerializer>(_options.SerializerType);

            _builder.RegisterInstance(ArrayPool<byte>.Create()); //register one arraypool for all components to share
            _builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share

            return this;
        }

        public IoC RegisterCRAComponents()
        {
            //No need to register types of VertexBase, as this gets instantiated by CRA
            RegisterAllConcreteClassesOfType<IAsyncShardedVertexInputEndpoint>();
            RegisterAllConcreteClassesOfType<IAsyncShardedVertexOutputEndpoint>();
            return this;
        }

        //public IoC RegisterOperatorConfiguration(IOperatorConfiguration config)
        //{
        //    config = config ?? throw new ArgumentNullException(nameof(config));

        //    var configType = config.GetType();

        //    _builder
        //        .RegisterInstance(config)
        //        .AsImplementedInterfaces()
        //        .SingleInstance();

        //    return this;
        //}

        private void RegisterConcreteClassAsDefined(Type concreteType, bool asSingleton = false)
        {
            var registration = _builder.RegisterType(concreteType).AsImplementedInterfaces().As(concreteType);
            _ = asSingleton ? registration.SingleInstance() : registration.InstancePerDependency();
        }

        private void RegisterConcreteClassAsType<T>(Type concreteType, bool asSingleton = false)
        {
            var registration = _builder.RegisterType(concreteType).As(typeof(T), concreteType);
            _ = asSingleton ? registration.SingleInstance() : registration.InstancePerDependency();
        }

        private void RegisterAllConcreteClassesOfType<T>(string inNamespace = "BlackSP", bool asSingleton = false)
        {
            var concreteTypes = _typesInRuntime
                .Where(p => p.IsAssignableTo<T>() && !p.IsInterface && !p.IsAbstract && p.IsInNamespace(inNamespace));

            foreach (var concreteType in concreteTypes)
            {
                RegisterConcreteClassAsType<T>(concreteType, asSingleton);
            }
        }

        //TODO: move to some typeloader class?
        private void LoadAllAvailableAssemblies()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Where(a => !a.IsDynamic).Select(a => a.Location).ToArray();

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var referencedPaths = Directory.GetFiles(assemblyDir, "*.dll");
            var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
            toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

        }
    }

}
