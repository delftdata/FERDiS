using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Kernel.Logging;
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
        /// Allows specifying handle for setting the log configuration passed to every vertex
        /// </summary>
        /// <param name="logging"></param>
        IApplicationBuilder ConfigureLogging(ILogConfiguration logging);

        /// <summary>
        /// TODO: give signature
        /// </summary>
        IApplicationBuilder ConfigureCheckpointing();

        Task<IApplication> Build();
    }

}
