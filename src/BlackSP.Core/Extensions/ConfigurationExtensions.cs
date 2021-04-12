using BlackSP.Kernel.Configuration;
using System;

namespace BlackSP.Core.Extensions
{
    public static class ConfigurationExtensions
    {

        [Obsolete("This method relies on GetHashCode and should eventually be changed for a runtime agnostic implementation")]
        public static int GetPartitionKeyForInstanceName(this IVertexConfiguration vertexConfig, string instanceName)
        {
            _ = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));

            foreach(var endpoint in vertexConfig.OutputEndpoints)
            {
                int i = 0;
                foreach(var remoteInstanceName in endpoint.RemoteInstanceNames)
                {
                    if(remoteInstanceName == instanceName)
                    {
                        return endpoint.GetConnectionKey(i).GetHashCode();//only used in coordinator so hashcode only gets calculated on a single machine
                    }
                    i++;
                }
            }
            throw new Exception($"Could not find partitionkey for instancename: {instanceName}");
        }

        [Obsolete("This method relies on GetHashCode and should eventually be changed for a runtime agnostic implementation")]
        public static (IEndpointConfiguration, int) GetTargetPairByPartitionKey(this IVertexConfiguration vertexConfig, int partitionKey)
        {
            _ = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));

            foreach (var endpoint in vertexConfig.OutputEndpoints)
            {
                int i = 0;
                foreach (var remoteInstanceName in endpoint.RemoteInstanceNames)
                {
                    var connectionKey = endpoint.GetConnectionKey(i);
                    if (connectionKey.GetHashCode() == partitionKey)
                    {
                        return (endpoint, i);
                    }
                    i++;
                }
            }
            throw new Exception($"Could not find connectionkey for partitionkey: {partitionKey}");

        }
    }
}
