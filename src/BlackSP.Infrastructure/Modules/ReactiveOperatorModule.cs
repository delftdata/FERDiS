using Autofac;
using BlackSP.Core;
using BlackSP.Core.Middlewares;
using BlackSP.Core.Models;
using BlackSP.Infrastructure.Controllers;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Text;

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
            builder.UseMessageReceiver();

            //control processor
            builder.RegisterType<ControlProcessController>().SingleInstance();
            builder.RegisterType<GenericMiddlewareDeliverer<ControlMessage>>().As<IMessageDeliverer<ControlMessage>>().SingleInstance();
            builder.AddControlMiddlewaresForWorker();

            //data processor
            builder.RegisterType<GenericMiddlewareDeliverer<DataMessage>>().As<IMessageDeliverer<DataMessage>>().SingleInstance();

            //control + data dispatcher
            builder.UseWorkerDispatcher();
            //TODO: middlewares
            builder.AddOperatorMiddleware<TShell, TOperator>();

            base.Load(builder);
        }
    }
}
