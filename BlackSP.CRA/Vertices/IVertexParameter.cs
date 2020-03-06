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
        /// Holds a type reference to the operator the target vertex should instantiate
        /// </summary>
        Type OperatorType { get; }

        /// <summary>
        /// The operator configuration required to instantiate
        /// the type provided in OperatorType
        /// </summary>
        Type OperatorConfiguration { get; }

        /// <summary>
        /// How many input endpoints to start
        /// </summary>
        int InputEndpointCount { get; }

        /// <summary>
        /// The input endpoint type the operator should be using
        /// </summary>
        Type InputEndpointType { get; }

        /// <summary>
        /// How many output endpoints to start
        /// </summary>
        int OutputEndpointCount { get; }

        /// <summary>
        /// The input endpoint type the operator should be using
        /// </summary>
        Type OutputEndpointType { get; }

        /// <summary>
        /// The serializer type the endpoints should use for sending events over the network
        /// </summary>
        Type SerializerType { get; }
    }
}
