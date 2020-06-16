using Autofac;
using BlackSP.Core;
using BlackSP.Core.MessageSources;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Controllers;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using BlackSP.Middlewares;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Modules
{
    public class SourceOperatorModule<TShell, TOperator, TEvent> : Module where TEvent : class, IEvent

    {
        //IHostConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            //_ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");

            builder.UseProtobufSerializer();
            builder.UseStreamingEndpoints();

            //collector only as control source
            builder.UseMessageReceiver(false);

            //data source (local source operator)
            builder.RegisterType<TOperator>().AsImplementedInterfaces();
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<SourceOperatorDataSource<TEvent>>().As<IMessageSource<DataMessage>>();

            //control processor
            builder.RegisterType<ControlProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<ControlMessage>>().As<IMessageDeliverer<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<DataProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<DataMessage>>().As<IMessageDeliverer<DataMessage>>().SingleInstance();

            //Note: consumer is expected to register data middlewares himself
            builder.RegisterType<PassthroughMiddleware>().AsImplementedInterfaces();
            //TODO: insert real middlewares


            //control + data dispatcher
            builder.UseWorkerDispatcher();            //TODO: middlewares?


            base.Load(builder);
        }
    }
}
