using Autofac;
using Autofac.Core;
using BlackSP.Checkpointing;
using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Kernel.Checkpointing;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AutofacCheckpointingExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        private static void OnActivated_CheckpointServiceRegistration(object sender, IActivatedEventArgs<object> context)
        {
            object obj = context.Instance;
            var manager = context.Context.Resolve<ICheckpointService>();
            var isRegistered = manager.RegisterObject(obj);
            //TODO: log information
        }

        public static ContainerBuilder UseCheckpointingService(this ContainerBuilder builder, ICheckpointConfiguration config, bool autoRegisterComponents = false)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = config ?? throw new ArgumentNullException(nameof(config));

            builder.RegisterInstance(config).AsImplementedInterfaces();

            builder.RegisterType<CheckpointDependencyTracker>().AsSelf();
            builder.RegisterType<ObjectRegistry>().AsSelf();
            builder.RegisterType<AzureBackedCheckpointStorage>().AsImplementedInterfaces();
            builder.RegisterType<RecoveryLineCalculator>().AsSelf();
            //above are the dependencies of the service below
            builder.RegisterType<CheckpointService>().As<ICheckpointService>().InstancePerLifetimeScope();

            if (autoRegisterComponents)
            {
                //include eventhandlers that ensure any DI resolved object is registered with the CheckpointService
                builder.ComponentRegistryBuilder.Registered += (object sender, ComponentRegisteredEventArgs e) => {
                    e.ComponentRegistration.Activated += OnActivated_CheckpointServiceRegistration;
                };
            }

            return builder;
        }
    }
}
