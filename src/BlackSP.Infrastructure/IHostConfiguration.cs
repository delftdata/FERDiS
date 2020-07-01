using BlackSP.OperatorShells;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure
{
    /// <summary>
    /// Model holding relevant types and configurations for a host instance for BlackSP
    /// </summary>
    public interface IHostConfiguration
    {

        /// <summary>
        /// The type of the autofac module that needs to be registered during vertex configuration
        /// </summary>
        Type StartupModule { get; }

        IVertexGraphConfiguration GraphConfiguration { get; }

        IVertexConfiguration VertexConfiguration { get; }


    }
}
