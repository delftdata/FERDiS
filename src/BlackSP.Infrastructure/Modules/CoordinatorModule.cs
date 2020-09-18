using Autofac;
using BlackSP.Core;
using BlackSP.Core.Processors;
using BlackSP.Core.Sources;
using BlackSP.Core.Middlewares;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Text;
using Serilog.Events;

namespace BlackSP.Infrastructure.Modules
{
    public class CoordinatorModule : Module
    {
        private readonly IHostConfiguration _configuration;

        public CoordinatorModule(IHostConfiguration hostConfiguration)
        {
            _configuration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.UseSerilog(_configuration.LogConfiguration, _configuration.VertexConfiguration.InstanceName);
            builder.UseCheckpointingService(_configuration.CheckpointingConfiguration);

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //sources (control only)
            builder.RegisterType<WorkerRequestSource>().As<ISource<ControlMessage>>();
            builder.UseReceiverMessageSource(false);

            builder.UseStatusMonitors();
            builder.UseStateManagers();

            //processor (control only)
            builder.RegisterType<ControlMessageProcessor>().SingleInstance();
            builder.RegisterType<MiddlewareInvocationPipeline<ControlMessage>>().As<IPipeline<ControlMessage>>().SingleInstance();

            //middlewares (control only - handles worker responses)
            builder.AddControlMiddlewaresForCoordinator();

            //dispatcher (control only)
            builder.UseCoordinatorDispatcher();
            //TODO: middlewares ??

            base.Load(builder);
        }
    }
}
