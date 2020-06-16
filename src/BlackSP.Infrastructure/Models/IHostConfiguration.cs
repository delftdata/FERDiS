using BlackSP.Core.OperatorShells;
using BlackSP.Kernel;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Models
{
    /// <summary>
    /// Model holding relevant types and configurations for a host instance for BlackSP
    /// </summary>
    public interface IHostConfiguration
    {
        /// <summary>
        /// Holds a type reference to the operator the target vertex should instantiate
        /// </summary>
        Type OperatorShellType { get; }

        /// <summary>
        /// The operator configuration required to instantiate
        /// the type provided in OperatorType
        /// </summary>
        Type OperatorType { get; }

        IVertexConfiguration VertexConfiguration { get; }
    }
}
