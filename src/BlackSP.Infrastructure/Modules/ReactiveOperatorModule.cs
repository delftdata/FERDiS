using Autofac;
using BlackSP.Core;
using BlackSP.Core.Controllers;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Kernel;

namespace BlackSP.Infrastructure.Modules
{
    public class ReactiveOperatorModule<TShell, TOperator> : Module
    {
        //IHostConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            //_ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //control + data collector & expose as source(s)
            builder.UseReceiverMessageSource();

            builder.UseWorkerMonitors();

            //control processor
            builder.RegisterType<ControlLayerProcessController>().SingleInstance();
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
