using BlackSP.Infrastructure.Configuration;
using BlackSP.InMemory.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.InMemory.Extensions
{
    public static class EdgeExtensions
    {
        public static IEnumerable<Connection> ToConnections(this Edge edge)
        {
            foreach(var fromInstanceName in edge.FromOperator.InstanceNames)
            {
                foreach(var toInstanceName in edge.ToOperator.InstanceNames)
                {
                    yield return new Connection
                    {
                        FromEndpointName = edge.FromEndpoint,
                        FromOperatorName = edge.FromOperator.OperatorName,
                        FromInstanceName = fromInstanceName,

                        ToEndpointName = edge.ToEndpoint,
                        ToOperatorName = edge.ToOperator.OperatorName,
                        ToInstanceName = toInstanceName,
                    };
                }
                
            }
        }
    }
}
