using Autofac;
using BlackSP.Core.Models;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Operators;
using BlackSP.Middlewares;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Infrastructure.Models;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacDataMiddlewareExtensions
    {
        public static ContainerBuilder AddOperatorMiddleware<TShell, TOperator>(this ContainerBuilder builder)
        {
            builder.RegisterType<TShell>().As<IOperatorShell>();
            builder.RegisterType<TOperator>().As<IOperator>();
            builder.RegisterType<OperatorMiddleware>().As<IMiddleware<DataMessage>>();

            return builder;
        }
    }
}
