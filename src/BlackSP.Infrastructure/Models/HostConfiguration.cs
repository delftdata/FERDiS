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
        /*public Type OperatorShellType => Type.GetType(_operatorShellTypeString);
        private string _operatorShellTypeString;

        public Type OperatorType => Type.GetType(_operatorTypeString);
        private string _operatorTypeString;
        */

        public Type StartupModule => Type.GetType(_startupModuleString);
        private string _startupModuleString;
        public IVertexConfiguration VertexConfiguration { get; set; }


        public HostConfiguration(Type startupModuleType, IVertexConfiguration vertexConfig)
        {
            //_operatorShellTypeString = operatorShellType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(operatorShellType));
            //_operatorTypeString = operatorType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(operatorType));
            _startupModuleString = startupModuleType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(startupModuleType));

            VertexConfiguration = vertexConfig;
        }
    }
}
