using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Infrastructure.Configuration;

namespace BlackSP.Infrastructure.Models
{
    /// <summary>
    /// Model class that holds data regarding an edge in an Vertex graph
    /// </summary>
    public class Edge
    {
        public IVertexBuilder FromVertex { get; private set; }
        public string FromEndpoint { get; private set; }

        public IVertexBuilder ToVertex { get; private set; }
        public string ToEndpoint { get; private set; }

        public Edge(IVertexBuilder fromVertex, string fromEndpoint, IVertexBuilder toVertex, string toEndpoint)
        {
            FromVertex = fromVertex ?? throw new ArgumentNullException(nameof(fromVertex));
            FromEndpoint = fromEndpoint ?? throw new ArgumentNullException(nameof(fromEndpoint));

            ToVertex = toVertex ?? throw new ArgumentNullException(nameof(toVertex));
            ToEndpoint = toEndpoint ?? throw new ArgumentNullException(nameof(toEndpoint));

        }
    }
}
