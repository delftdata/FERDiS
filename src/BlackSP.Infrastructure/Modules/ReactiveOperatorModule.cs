using Autofac;
using BlackSP.Core;
using BlackSP.Core.Processors;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Kernel;
using Serilog.Events;
using System;
using BlackSP.Infrastructure.Layers.Control;

namespace BlackSP.Infrastructure.Modules
{
    public class ReactiveOperatorModule<TShell, TOperator> : Module
    {
        private readonly IHostConfiguration _configuration;

        public ReactiveOperatorModule(IHostConfiguration hostConfiguration)
        {
            _configuration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        }

        protected override void Load(ContainerBuilder builder)
        {
            //_ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");
            builder.UseSerilog(_configuration.LogConfiguration, _configuration.VertexConfiguration.InstanceName);
            builder.UseCheckpointingService(_configuration.CheckpointingConfiguration, true);

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();
            builder.UseStatusMonitors();

            //control processor
            builder.UseControlLayer();
            builder.AddControlLayerMessageHandlersForWorker();

            //data processor
            builder.UseDataLayer();
            builder.AddDataLayerMessageHandlersForWorker<TShell, TOperator>(_configuration.CheckpointingConfiguration.CoordinationMode);

            base.Load(builder);
        }
    }
}
