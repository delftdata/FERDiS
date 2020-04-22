using Autofac;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BlackSP.Infrastructure.IoC
{

    public class DependencyContainerBuilder
    {
        private IHostParameter _options;
        private ContainerBuilder _builder;
        private IEnumerable<Type> _typesInRuntime;
        public DependencyContainerBuilder(IHostParameter options)
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
            container.IsRegistered<IOperatorShell>();
            container.IsRegistered<IOutputEndpoint>();
            container.IsRegistered<IInputEndpoint>();
            container.IsRegistered<IOperator>();
            return container;
        }

        public DependencyContainerBuilder RegisterBlackSPComponents()
        {
            RegisterConcreteClassAsType<IOperatorShell>(_options.OperatorShellType, true);
            RegisterConcreteClassAsDefined(_options.OperatorType, true);

            RegisterConcreteClassAsType<IInputEndpoint>(_options.InputEndpointType);
            RegisterConcreteClassAsType<IOutputEndpoint>(_options.OutputEndpointType);
            RegisterConcreteClassAsType<ISerializer>(_options.SerializerType);

            //TODO: register logger?

            _builder.RegisterInstance(ArrayPool<byte>.Create()); //register one arraypool for all components to share
            _builder.RegisterInstance(new RecyclableMemoryStreamManager()); //register one memorystreampool for all components to share

            return this;
        }

        public DependencyContainerBuilder RegisterConcreteClassAsDefined(Type concreteType, bool asSingleton = false)
        {
            var registration = _builder.RegisterType(concreteType).AsImplementedInterfaces().As(concreteType);
            _ = asSingleton ? registration.SingleInstance() : registration.InstancePerDependency();
            return this;
        }

        public DependencyContainerBuilder RegisterConcreteClassAsType<T>(Type concreteType, bool asSingleton = false)
        {
            var registration = _builder.RegisterType(concreteType).As(typeof(T), concreteType);
            _ = asSingleton ? registration.SingleInstance() : registration.InstancePerDependency();
            return this;
        }

        public DependencyContainerBuilder RegisterAllConcreteClassesOfType<T>(string inNamespace = "BlackSP", bool asSingleton = false)
        {
            var concreteTypes = _typesInRuntime
                .Where(p => p.IsAssignableTo<T>() && !p.IsInterface && !p.IsAbstract && p.IsInNamespace(inNamespace));

            foreach (var concreteType in concreteTypes)
            {
                RegisterConcreteClassAsType<T>(concreteType, asSingleton);
            }
            return this;
        }
        
        /// <summary>
        /// Loads all assembly files found in the executing assembly's folder
        /// </summary>
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
