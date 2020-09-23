using Autofac;
using BlackSP.Core.Handlers;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using BlackSP.Infrastructure.Layers.Control;
using BlackSP.Infrastructure.Layers.Data.Sources;
using BlackSP.Infrastructure.Layers.Data;

namespace BlackSP.Infrastructure.Modules
{
    public class SourceOperatorModule<TShell, TOperator, TEvent> : Module 
        where TEvent : class, IEvent
    {
        private readonly IHostConfiguration _configuration;

        public SourceOperatorModule(IHostConfiguration hostConfiguration)
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
            builder.UseDataLayer(false);
            //add alternative data source (local source operator)
            builder.RegisterType<TOperator>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TShell>().As<IOperatorShell>().SingleInstance();
            builder.RegisterType<SourceOperatorEventSource<TEvent>>().As<ISource<DataMessage>>().SingleInstance();

            //middlewares
            builder.AddDataLayerMessageHandlersForSource(_configuration.CheckpointingConfiguration.CoordinationMode);

            base.Load(builder);
        }
    }
}
