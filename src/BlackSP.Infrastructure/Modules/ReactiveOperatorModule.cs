using Autofac;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Control.Sources;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Logging;
using System;

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
            builder.UseLogging(_configuration.LogConfiguration, _configuration.VertexConfiguration.InstanceName);
            builder.UseCheckpointing(_configuration.CheckpointingConfiguration, true);

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
