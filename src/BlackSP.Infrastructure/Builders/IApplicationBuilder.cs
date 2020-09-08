using BlackSP.Checkpointing;
using BlackSP.Infrastructure.Builders.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Builders
{
    public interface IApplicationBuilder
    {

        /// <summary>
        /// Allows specifying handle for configuring the operator vertex graph
        /// </summary>
        /// <param name="builder"></param>
        IApplicationBuilder ConfigureOperators(Action<IOperatorVertexGraphBuilder> builder);

        /// <summary>
        /// Sets the log configuration passed to every vertex
        /// </summary>
        /// <param name="logging"></param>
        IApplicationBuilder ConfigureLogging(ILogConfiguration logging);

        /// <summary>
        /// Sets the checkpoint configuration passed to every vertex
        /// </summary>
        IApplicationBuilder ConfigureCheckpointing(ICheckpointConfiguration checkpointConfig);

        /// <summary>
        /// Build the IApplication
        /// </summary>
        /// <returns></returns>
        Task<IApplication> Build();
    }

}
