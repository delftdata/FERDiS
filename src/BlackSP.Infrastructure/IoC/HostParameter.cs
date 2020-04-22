using BlackSP.Core.OperatorShells;
using BlackSP.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.IoC
{
    [Serializable]
    public class HostParameter : IHostParameter
    {
        public Type OperatorShellType => Type.GetType(_operatorShellTypeString);
        private string _operatorShellTypeString;

        public Type OperatorType => Type.GetType(_operatorTypeString);
        private string _operatorTypeString;

        public Type InputEndpointType => Type.GetType(_inputEndpointTypeString);
        private string _inputEndpointTypeString;
        public string[] InputEndpointNames { get; set; }
        
        public Type OutputEndpointType => Type.GetType(_outputEndpointTypeString);
        private string _outputEndpointTypeString;
        public string[] OutputEndpointNames { get; set; }
        
        public Type SerializerType => Type.GetType(_serializerTypeString);
        private string _serializerTypeString;

        public HostParameter(Type operatorShellType, Type operatorType, string[] inputNames, Type inputEndpointType, string[] outputNames, Type outputEndpointType, Type serializerType)
        {
            _operatorShellTypeString = operatorShellType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(operatorShellType));
            _operatorTypeString = operatorType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(operatorType));

            InputEndpointNames = inputNames;
            _inputEndpointTypeString = inputEndpointType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(inputEndpointType));

            OutputEndpointNames = outputNames;
            _outputEndpointTypeString = outputEndpointType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(outputEndpointType));

            _serializerTypeString = serializerType?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(serializerType));
        }
    }
}
