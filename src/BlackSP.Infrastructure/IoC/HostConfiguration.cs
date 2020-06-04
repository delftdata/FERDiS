using BlackSP.Core.OperatorShells;
using BlackSP.Infrastructure.Configuration;
using BlackSP.Kernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.IoC
{
    [Serializable]
    public class HostConfiguration : IHostConfiguration
    {
        public Type OperatorShellType => Type.GetType(_operatorShellTypeString);
        private string _operatorShellTypeString;

        public Type OperatorType => Type.GetType(_operatorTypeString);
        private string _operatorTypeString;
   
        public IVertexConfiguration VertexConfiguration { get; set; }


        public HostConfiguration(Type operatorShellType, Type operatorType, IVertexConfiguration vertexConfig)
        {
            _operatorShellTypeString = operatorShellType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(operatorShellType));
            _operatorTypeString = operatorType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(operatorType));

            VertexConfiguration = vertexConfig;
        }
    }
}
