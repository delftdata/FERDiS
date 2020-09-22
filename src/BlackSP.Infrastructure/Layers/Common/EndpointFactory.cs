using Autofac;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Models;
using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Layers.Common
{
    public class EndpointFactory
    {

        private readonly ILifetimeScope _scope;

        public EndpointFactory(ILifetimeScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public IInputEndpoint ConstructInputEndpoint(IEndpointConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));

            if(config.IsControl)
            {
                return _scope.Resolve<InputEndpoint<ControlMessage>.Factory>().Invoke(config.LocalEndpointName);
            } 
            else
            {
                return _scope.Resolve<InputEndpoint<DataMessage>.Factory>().Invoke(config.LocalEndpointName);
            }
        }

        public IOutputEndpoint ConstructOutputEndpoint(IEndpointConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));

            if (config.IsControl)
            {
                return _scope.Resolve<OutputEndpoint<ControlMessage>.Factory>().Invoke(config.LocalEndpointName);
            }
            else
            {
                return _scope.Resolve<OutputEndpoint<DataMessage>.Factory>().Invoke(config.LocalEndpointName);
            }
        }

    }
}
