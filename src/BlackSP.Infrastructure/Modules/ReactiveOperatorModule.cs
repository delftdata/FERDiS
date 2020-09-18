using Autofac;
using BlackSP.Core;
using BlackSP.Core.Processors;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Kernel;
using Serilog.Events;
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
            builder.UseSerilog(_configuration.LogConfiguration, _configuration.VertexConfiguration.InstanceName);
            builder.UseCheckpointingService(_configuration.CheckpointingConfiguration, true);

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //control + data collector & expose as source(s)
            builder.UseReceiverMessageSource();

            builder.UseStatusMonitors();

            //control processor
            builder.RegisterType<ControlMessageProcessor>().SingleInstance();
            builder.RegisterType<MiddlewareInvocationPipeline<ControlMessage>>().As<IPipeline<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<MiddlewareInvocationPipeline<DataMessage>>().As<IPipeline<DataMessage>>().SingleInstance();

            //control + data dispatcher
            builder.UseWorkerDispatcher();
            //TODO: middlewares
            builder.AddOperatorMiddlewareForWorker<TShell, TOperator>();

            base.Load(builder);
        }
    }
}
