using Autofac;
using Autofac.Core;
using BlackSP.Checkpointing;
using BlackSP.Checkpointing.Core;
using BlackSP.Checkpointing.Persistence;
using BlackSP.Checkpointing.Protocols;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
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
            var service = context.Context.Resolve<ICheckpointService>();
            service.RegisterObject(context.Instance);
        }

        /// <summary>
        /// Registers checkpointing service, message logging service and protocol types.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="config"></param>
        /// <param name="autoRegisterComponents"></param>
        /// <returns></returns>
        public static ContainerBuilder UseCheckpointing(this ContainerBuilder builder, ICheckpointConfiguration config, bool autoRegisterComponents = false)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            _ = config ?? throw new ArgumentNullException(nameof(config));

            builder.RegisterInstance(config).AsImplementedInterfaces();

            builder.RegisterType<CheckpointDependencyTracker>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<ObjectRegistry>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<AzureBackedCheckpointStorage>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<RecoveryLineCalculator>().AsSelf();
            //above are the dependencies of the service below
            builder.RegisterType<CheckpointService>().As<ICheckpointService>().SingleInstance();

            if (autoRegisterComponents)
            {
                //include eventhandlers that ensure any DI resolved object is registered with the CheckpointService
                builder.ComponentRegistryBuilder.Registered += (object sender, ComponentRegisteredEventArgs e) => {
                    e.ComponentRegistration.Activated += OnActivated_CheckpointServiceRegistration;
                };
            }

            if(config.CoordinationMode != CheckpointCoordinationMode.Coordinated)
            {
                //logging service implementation for non coordinated checkpointing
                builder.RegisterType<MessageLoggingService<byte[]>>().As<IMessageLoggingService<byte[]>>().SingleInstance();
            }
            
            
            //protocol types
            builder.RegisterType<ChandyLamportProtocol>().AsSelf();
            builder.RegisterType<UncoordinatedProtocol>().AsSelf();
            builder.RegisterType<HMNRProtocol>().AsSelf().SingleInstance();


            return builder;
        }
    }
}
