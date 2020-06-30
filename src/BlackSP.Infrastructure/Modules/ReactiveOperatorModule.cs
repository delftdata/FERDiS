using Autofac;
using BlackSP.Core;
using BlackSP.Core.Controllers;
using BlackSP.Core.Models;
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
            builder.UseMessageReceiver();

            //control processor
            builder.RegisterType<MultiSourceProcessController<ControlMessage>>().SingleInstance();
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
