using BlackSP.Core.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Vertices
{
    public class VertexParameter : IVertexParameter
    {
        //yeah public setters .. but some parameters get lost in serialization if we make them private
        public Type OperatorType { get; set; }
        public Type OperatorConfiguration { get; set; }
        public Type InputEndpointType { get; set; }
        public int InputEndpointCount { get; set; }
        public Type OutputEndpointType { get; set; }
        public int OutputEndpointCount { get; set; }
        public Type SerializerType { get; set; }

        public VertexParameter(Type operatorType, Type operatorConfigType, int inputCount, Type inputEndpointType, int outputCount, Type outputEndpointType, Type serializerType)
        {
            OperatorType = operatorType;
            OperatorConfiguration = operatorConfigType;

            InputEndpointCount = inputCount;
            InputEndpointType = inputEndpointType;

            OutputEndpointCount = outputCount;
            OutputEndpointType = outputEndpointType;

            SerializerType = serializerType;
        }
    }
}
