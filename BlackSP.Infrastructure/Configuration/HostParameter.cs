using BlackSP.Core.OperatorSockets;
using BlackSP.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Configuration
{
    public class HostParameter : IHostParameter
    {
        //using public setters because some parameters get lost in cra's serialization if we make them private
        public Type OperatorType { get; set; }
        public Type OperatorConfiguration { get; set; }
        public Type InputEndpointType { get; set; }
        public string[] InputEndpointNames { get; set; }
        public Type OutputEndpointType { get; set; }
        public string[] OutputEndpointNames { get; set; }
        public Type SerializerType { get; set; }

        public HostParameter(Type operatorType, Type operatorConfigType, string[] inputNames, Type inputEndpointType, string[] outputNames, Type outputEndpointType, Type serializerType)
        {
            OperatorType = operatorType;
            OperatorConfiguration = operatorConfigType;

            InputEndpointNames = inputNames;
            InputEndpointType = inputEndpointType;

            OutputEndpointNames = outputNames;
            OutputEndpointType = outputEndpointType;

            SerializerType = serializerType;
        }
    }
}
