using BlackSP.Core.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Vertices
{
    public class VertexParameter : IVertexParameter
    {
        //TODO: move setters to constructor and only use getters
        public int InputEndpointCount { get; set; }
        public int OutputEndpointCount { get; set; }
        public Type OperatorType { get; set; }
        public IOperatorConfiguration OperatorConfiguration { get; set; }
    }
}
