using BlackSP.Core.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Vertices
{
    public class VertexParameter : IVertexParameter
    {
        public Type OperatorType { get; set; }
        public IOperatorConfiguration OperatorConfiguration { get; set; }
    }
}
