﻿using BlackSP.Kernel.Endpoints;
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
        /// Note that this includes all downstream operators, not just the direct descendants.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesDownstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var downstreamInstances = new List<string>();

            var children = graphConfig.InstanceConnections.Where(t => t.Item1 == instanceName).Select(t => t.Item2);

            foreach(var child in children)
            {
                downstreamInstances.Add(child);

                var grandChildren = graphConfig.GetAllInstancesDownstreamOf(child);
                foreach(var grandChild in grandChildren)
                {
                    downstreamInstances.Add(grandChild);
                }
            }
            return downstreamInstances;
        }

        public static IEnumerable<string> GetAllConnectionKeys(this IEndpointConfiguration endpointConfig)
        {
            _ = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));

            for (int i = 0; i < endpointConfig.RemoteInstanceNames.Count(); i++)
            {
                yield return endpointConfig.GetConnectionKey(i);
            }
        }

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
                        return endpoint.GetConnectionKey(i).GetHashCode();
                    }
                    i++;
                }
            }
            throw new Exception($"Could not find partitionkey for instancename: {instanceName}");
        }

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
