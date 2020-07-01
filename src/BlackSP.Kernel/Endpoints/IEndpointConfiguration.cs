using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Endpoints
{
    public interface IEndpointConfiguration
    {
        /// <summary>
        /// The local endpoint name
        /// </summary>
        string LocalEndpointName { get; }

        /// <summary>
        /// The remote vertex name
        /// </summary>
        string RemoteVertexName { get; set; }

        /// <summary>
        /// The remote endpoint name
        /// </summary>
        string RemoteEndpointName { get; }

        /// <summary>
        /// The instance names of each shard of the remote vertex (index == shardId)
        /// </summary>
        IEnumerable<string> RemoteInstanceNames { get; }

        /// <summary>
        /// Indicator of endpoint being a control message endpoint<br/>
        /// false indicates it being a data message endpoint
        /// </summary>
        bool IsControl { get; }

        /// <summary>
        /// Utility method to get a unique key string representing a single connection within the endpoint
        /// </summary>
        /// <param name="shardId"></param>
        /// <returns></returns>
        string GetConnectionKey(int shardId);

    }
}
