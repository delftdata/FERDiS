using BlackSP.Core.Operators;
using BlackSP.Interfaces.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Vertices
{
    public interface IVertexParameter
    {
        /// <summary>
        /// How many input endpoints to start
        /// </summary>
        int InputEndpointCount { get; set; }

        /// <summary>
        /// How many output endpoints to start
        /// </summary>
        int OutputEndpointCount { get; set; }
        
        /// <summary>
        /// Holds a type reference to the operator the target vertex should instantiate
        /// </summary>
        Type OperatorType { get; set; }

        /// <summary>
        /// Holds an object reference to the operator configuration required to instantiate
        /// the type provided in OperatorType
        /// </summary>
        IOperatorConfiguration OperatorConfiguration { get; set; }
    }
}
