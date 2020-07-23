using Autofac;
using BlackSP.Core;
using BlackSP.Core.Controllers;
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
        //IHostConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            //_ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");
            builder.UseSerilog(LogEventLevel.Verbose, LogTargetFlags.Console);
            builder.UseCheckpointingService();

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //sources (control only)
            builder.RegisterType<WorkerStateChangeSource>().As<ISource<ControlMessage>>();
            builder.UseReceiverMessageSource(false);

            builder.UseCoordinatorMonitors();

            //processor (control only)
            builder.RegisterType<ControlLayerProcessController>().SingleInstance();
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
