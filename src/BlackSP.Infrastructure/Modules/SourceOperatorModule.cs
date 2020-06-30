using Autofac;
using BlackSP.Core;
using BlackSP.Core.Controllers;
using BlackSP.Core.MessageSources;
using BlackSP.Core.Middlewares;
using BlackSP.Core.Models;
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

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //receiver only as control source
            builder.UseMessageReceiver(false);

            //data source (local source operator)
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<SourceOperatorDataSource<TEvent>>().As<IMessageSource<DataMessage>>();

            //control processor
            builder.RegisterType<MultiSourceProcessController<ControlMessage>>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<ControlMessage>>().As<IMessageDeliverer<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<GenericMiddlewareDeliverer<DataMessage>>().As<IMessageDeliverer<DataMessage>>().SingleInstance();

            //Note: consumer is expected to register data middlewares himself
            builder.RegisterType<PassthroughMiddleware<DataMessage>>().AsImplementedInterfaces();
            //TODO: insert real middlewares


            //control + data dispatcher
            builder.UseWorkerDispatcher();            
            //TODO: middlewares?

            base.Load(builder);
        }
    }
}
