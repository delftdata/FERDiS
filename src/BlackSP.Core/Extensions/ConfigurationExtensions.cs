using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Core.Extensions
{
    public static class ConfigurationExtensions
    {

        /// <summary>
        /// Returns an enumerable of instancenames that lie downstream of the given instance with supplied name.<br/>
        /// (instances are not necessarily workers only!)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <param name="excludeGrandChildren"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesDownstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName, bool excludeGrandChildren)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var children = graphConfig.InstanceConnections.Where(t => t.Item1 == instanceName).Select(t => t.Item2);
            var grandChildren = excludeGrandChildren ? Enumerable.Empty<string>() : children.SelectMany(child => graphConfig.GetAllInstancesDownstreamOf(child, excludeGrandChildren));

            return children.Concat(grandChildren);
        }

        /// <summary>
        /// Returns an enumerable of instancenames that are upstream of the given instance with supplied name.<br/>
        /// (instances are not necessarily workers only!)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <param name="excludeGrandParents"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesUpstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName, bool excludeGrandParents)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var parents = graphConfig.InstanceConnections.Where(t => t.Item2 == instanceName).Select(t => t.Item1);
            var grandParents = excludeGrandParents ? Enumerable.Empty<string>() : parents.SelectMany(parent => graphConfig.GetAllInstancesUpstreamOf(parent, excludeGrandParents));
            return parents;
        }

        public static IEnumerable<string> GetAllConnectionKeys(this IEndpointConfiguration endpointConfig)
        {
            _ = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));

            for (int i = 0; i < endpointConfig.RemoteInstanceNames.Count(); i++)
            {
                yield return endpointConfig.GetConnectionKey(i);
            }
        }

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
        public static string GetConnectionKeyByPartitionKey(this IVertexConfiguration vertexConfig, int partitionKey)
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
                        return connectionKey;
                    }
                    i++;
                }
            }
            throw new Exception($"Could not find connectionkey for partitionkey: {partitionKey}");

        }
    }
}
