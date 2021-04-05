using Autofac;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Control.Sources;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Logging;

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
            builder.UseLogging(_configuration.LogConfiguration, _configuration.VertexConfiguration.InstanceName);
            builder.UseCheckpointingService(_configuration.CheckpointingConfiguration);

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();
            builder.UseStatusMonitors();
            builder.UseStateManagers();
            
            //control processor
            builder.UseControlLayer();
            //add more ControlMessage sources
            builder.RegisterType<WorkerRequestSource>().As<ISource<ControlMessage>>();
            if(_configuration.CheckpointingConfiguration.CoordinationMode == CheckpointCoordinationMode.Coordinated)
            {
                builder.RegisterType<CoordinatedCheckpointingInitiationSource>().As<ISource<ControlMessage>>();
            }
            builder.AddControlLayerMessageHandlersForCoordinator();

            base.Load(builder);
        }
    }
}
