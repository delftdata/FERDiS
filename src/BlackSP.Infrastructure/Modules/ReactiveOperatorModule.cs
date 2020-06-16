using Autofac;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Modules
{
    public class ReactiveOperatorModule<TShell, TOperator> : Module
    {
        IHostConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            _ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");

            builder.UseWorkerConfiguration();
            //TODO: middlewares
            builder.AddOperatorMiddleware<TShell, TOperator>();

            base.Load(builder);
        }
    }
}
