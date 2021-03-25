using BlackSP.OperatorShells;
using BlackSP.Kernel;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Configuration;

namespace BlackSP.Infrastructure
{
    /// <summary>
    /// Model containing configurations for a vertex instance host
    /// </summary>
    public interface IHostConfiguration
    {

        /// <summary>
        /// The type of the autofac module that needs to be registered during vertex configuration
        /// </summary>
        Type StartupModule { get; }

        IVertexGraphConfiguration GraphConfiguration { get; }

        IVertexConfiguration VertexConfiguration { get; }

        ILogConfiguration LogConfiguration { get; }

        ICheckpointConfiguration CheckpointingConfiguration { get; }
    }
}
