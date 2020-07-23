using Autofac;
using BlackSP.Core;
using BlackSP.Core.Controllers;
using BlackSP.Core.Sources;
using BlackSP.Core.Middlewares;
using BlackSP.Core.Models;
using BlackSP.Core.Pipelines;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;

namespace BlackSP.Infrastructure.Modules
{
    public class SourceOperatorModule<TShell, TOperator, TEvent> : Module 
        where TEvent : class, IEvent
    {
        //IHostConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            //_ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");
            builder.UseCheckpointingService(true);

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //receiver only as control source
            builder.UseReceiverMessageSource(false);
            
            builder.UseWorkerMonitors();
            
            //data source (local source operator)
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<SourceOperatorDataSource<TEvent>>().As<ISource<DataMessage>>();

            //control processor
            builder.RegisterType<ControlLayerProcessController>().SingleInstance();
            builder.RegisterType<MiddlewareInvocationPipeline<ControlMessage>>().As<IPipeline<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<MiddlewareInvocationPipeline<DataMessage>>().As<IPipeline<DataMessage>>().SingleInstance();

            //middlewares
            builder.RegisterType<PassthroughMiddleware<DataMessage>>().AsImplementedInterfaces();
            //TODO: insert middlewares


            //control + data dispatcher
            builder.UseWorkerDispatcher();            
            //TODO: middlewares?

            base.Load(builder);
        }
    }
}
