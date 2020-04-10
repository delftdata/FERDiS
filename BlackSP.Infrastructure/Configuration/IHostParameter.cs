using BlackSP.Core.OperatorSockets;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Configuration
{
    public interface IHostParameter
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
        string[] InputEndpointNames { get; }

        /// <summary>
        /// The input endpoint type the operator should be using
        /// </summary>
        Type InputEndpointType { get; }

        /// <summary>
        /// How many output endpoints to start
        /// </summary>
        string[] OutputEndpointNames { get; }

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
