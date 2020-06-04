using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel
{
    public interface IVertexConfiguration
    {
        string OperatorName { get; }

        string InstanceName { get; }

        /// <summary>
        /// Configuration of input endpoints
        /// </summary>
        ICollection<IEndpointConfiguration> InputEndpoints { get; }

        /// <summary>
        /// Configuration of output endpoints.
        /// </summary>
        ICollection<IEndpointConfiguration> OutputEndpoints { get; }
    }
}
