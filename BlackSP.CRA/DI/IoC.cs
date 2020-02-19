using Autofac;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Operators;
using BlackSP.Interfaces.Serialization;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.DI
{
    public class IoC
    {
        private ContainerBuilder _builder;

        public IoC()
        {
            _builder = new ContainerBuilder();
        }

        public IContainer BuildContainer()
        {
            return _builder.Build();
        }

        public IoC RegisterOperator(Type operatorType)
        {
            _builder
                .RegisterType(operatorType)
                .As<IOperator>()
                .SingleInstance();
            return this;
        }

        public IoC RegisterInputEndpoint(Type inputEndpointType)
        {
            _builder
                .RegisterType(inputEndpointType)
                .As<IInputEndpoint>()
                .As<IAsyncVertexInputEndpoint>()
                .SingleInstance();
            return this;
        }

        public IoC RegisterOutputEndpoint(Type outputEndpointType)
        {
            _builder
                .RegisterType(outputEndpointType)
                .As<IOutputEndpoint>()
                .As<IAsyncVertexOutputEndpoint>()
                .SingleInstance();
            return this;
        }

        public IoC RegisterSerializer(Type serializerType)
        {
            _builder
                .RegisterType(serializerType)
                .As<ISerializer>()
                .InstancePerDependency();
            return this;
        }
    }
}
