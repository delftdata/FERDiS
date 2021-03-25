using Autofac;
using BlackSP.Core.Endpoints;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Endpoints;
using System;

namespace BlackSP.Infrastructure.Factories
{
    public class EndpointFactory
    {

        private readonly ILifetimeScope _scope;

        public EndpointFactory(ILifetimeScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public IInputEndpoint ConstructInputEndpoint(IEndpointConfiguration config, bool supportTimeout = true)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            if(supportTimeout)
            {
                if (config.IsControl)
                {
                    return _scope.Resolve<FlushableTimeoutInputEndpoint<ControlMessage>.Factory>().Invoke(config.LocalEndpointName);
                }
                else
                {
                    return _scope.Resolve<FlushableTimeoutInputEndpoint<DataMessage>.Factory>().Invoke(config.LocalEndpointName);
                }
            }
            else
            {
                if (config.IsControl)
                {
                    return _scope.Resolve<InputEndpoint<ControlMessage>.Factory>().Invoke(config.LocalEndpointName);
                }
                else
                {
                    return _scope.Resolve<InputEndpoint<DataMessage>.Factory>().Invoke(config.LocalEndpointName);
                }
            }
            
        }

        public IOutputEndpoint ConstructOutputEndpoint(IEndpointConfiguration config, bool supportTimeout = true)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            if(supportTimeout)
            {
                if (config.IsControl)
                {
                    return _scope.Resolve<FlushableTimeoutOutputEndpoint<ControlMessage>.Factory>().Invoke(config.LocalEndpointName);
                }
                else
                {
                    return _scope.Resolve<FlushableTimeoutOutputEndpoint<DataMessage>.Factory>().Invoke(config.LocalEndpointName);
                }
            } 
            else
            {
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
}
