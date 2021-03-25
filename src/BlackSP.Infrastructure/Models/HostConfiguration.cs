using BlackSP.Checkpointing;
using BlackSP.Kernel.Configuration;
using System;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]
    public class HostConfiguration : IHostConfiguration
    {

        public Type StartupModule => Type.GetType(_startupModuleString);
        private string _startupModuleString;
        
        public IVertexConfiguration VertexConfiguration { get; set; }

        public IVertexGraphConfiguration GraphConfiguration { get; set; }

        public ILogConfiguration LogConfiguration { get; set; }

        public ICheckpointConfiguration CheckpointingConfiguration { get; set; }

        public HostConfiguration(Type startupModuleType, IVertexGraphConfiguration graphConfig, IVertexConfiguration vertexConfig, 
            ILogConfiguration logConfig, ICheckpointConfiguration checkpointingConfig)
        {

            _startupModuleString = startupModuleType?.AssemblyQualifiedName;
            GraphConfiguration = graphConfig;
            VertexConfiguration = vertexConfig;
            LogConfiguration = logConfig;
            CheckpointingConfiguration = checkpointingConfig;
        }
    }
}
