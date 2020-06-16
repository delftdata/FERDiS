﻿using Autofac;
using BlackSP.Infrastructure.Extensions;
using BlackSP.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Modules
{
    public class CoordinatorModule : Module
    {
        IHostConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            _ = Configuration ?? throw new NullReferenceException($"property {nameof(Configuration)} has not been set");

            builder.UseCoordinatorConfiguration();
            //TODO: middlewares ??

            base.Load(builder);
        }
    }
}
