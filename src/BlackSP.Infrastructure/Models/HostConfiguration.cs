using BlackSP.OperatorShells;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]
    public class HostConfiguration : IHostConfiguration
    {

        public Type StartupModule => Type.GetType(_startupModuleString);
        private string _startupModuleString;
        
        public IVertexConfiguration VertexConfiguration { get; set; }

        public IVertexGraphConfiguration GraphConfiguration { get; set; }

        public HostConfiguration(Type startupModuleType, IVertexGraphConfiguration graphConfig, IVertexConfiguration vertexConfig)
        {
            
            _startupModuleString = startupModuleType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(startupModuleType));
            GraphConfiguration = graphConfig;
            VertexConfiguration = vertexConfig;
        }
    }
}
