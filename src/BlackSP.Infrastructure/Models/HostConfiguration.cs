using BlackSP.OperatorShells;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Kernel.Logging;

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

        //public object CheckpointingConfiguration => throw new NotImplementedException();

        public HostConfiguration(Type startupModuleType, IVertexGraphConfiguration graphConfig, IVertexConfiguration vertexConfig, ILogConfiguration logConfig)
        {

            _startupModuleString = startupModuleType?.AssemblyQualifiedName;// ?? throw new ArgumentNullException(nameof(startupModuleType));
            GraphConfiguration = graphConfig;// ?? throw new ArgumentNullException(nameof(graphConfig));
            VertexConfiguration = vertexConfig;// ?? throw new ArgumentNullException(nameof(vertexConfig));
            LogConfiguration = logConfig;// ?? throw new ArgumentNullException(nameof(logConfig));
        }
    }
}
