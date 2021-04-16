using BlackSP.Kernel.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Kernel.Configuration
{
    public static class Extensions
    {

        /// <summary>
        /// Returns an enumerable of instancenames that lie downstream of the given instance with supplied name.<br/>
        /// (ignores coordinator)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <param name="excludeDescendants"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesDownstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName, bool excludeDescendants)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var downstreams = graphConfig.InstanceConnections.Where(t => t.Item1 == instanceName).Select(t => t.Item2).Where(name => !name.Contains("coordinator"));
            var descendants = excludeDescendants ? Enumerable.Empty<string>() : downstreams.SelectMany(child => graphConfig.GetAllInstancesDownstreamOf(child, downstreams.Concat(Enumerable.Repeat(instanceName,1))));

            return downstreams.Concat(descendants);
        }

        /// <summary>
        /// Returns an enumerable of worker instancenames that lie downstream of the given instance with supplied name.<br/>
        /// (ignores coordinator)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <param name="excludeDescendants"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesDownstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName, IEnumerable<string> anchestors)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var children = graphConfig.InstanceConnections.Where(t => t.Item1 == instanceName).Select(t => t.Item2).Where(name => !name.Contains("coordinator") && !anchestors.Contains(name));
            var descendants = children.Where(child => !anchestors.Contains(child))
                          .SelectMany(child => graphConfig.GetAllInstancesDownstreamOf(child, anchestors.Concat(children)));

            return children.Concat(descendants);
        }

        /// <summary>
        /// Returns an enumerable of instancenames that lie upstream of the given instance with supplied name.<br/>
        /// (ignores coordinator)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <param name="excludeDescendants"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesUpstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName, bool excludeDescendants)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var upstreams = graphConfig.InstanceConnections.Where(t => t.Item2 == instanceName).Select(t => t.Item1).Where(name => !name.Contains("coordinator"));
            var descendants = excludeDescendants ? Enumerable.Empty<string>() : upstreams.SelectMany(child => graphConfig.GetAllInstancesUpstreamOf(child, upstreams.Concat(Enumerable.Repeat(instanceName, 1))));

            return upstreams.Concat(descendants);
        }

        /// <summary>
        /// Returns an enumerable of worker instancenames that lie upstream of the given instance with supplied name.<br/>
        /// (ignores coordinator)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="instanceName"></param>
        /// <param name="excludeDescendants"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllInstancesUpstreamOf(this IVertexGraphConfiguration graphConfig, string instanceName, IEnumerable<string> anchestors)
        {
            _ = graphConfig ?? throw new ArgumentNullException(nameof(graphConfig));

            var children = graphConfig.InstanceConnections.Where(t => t.Item2 == instanceName).Select(t => t.Item1).Where(name => !name.Contains("coordinator") && !anchestors.Contains(name));
            var descendants = children.Where(child => !anchestors.Contains(child))
                          .SelectMany(child => graphConfig.GetAllInstancesUpstreamOf(child, anchestors.Concat(children)));

            return children.Concat(descendants);
        }

        public static IEnumerable<string> GetAllConnectionKeys(this IEndpointConfiguration endpointConfig)
        {
            _ = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));

            for (int i = 0; i < endpointConfig.RemoteInstanceNames.Count(); i++)
            {
                yield return endpointConfig.GetConnectionKey(i);
            }
        }
    }
}
