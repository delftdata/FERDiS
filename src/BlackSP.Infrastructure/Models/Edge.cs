﻿using BlackSP.Kernel.Endpoints;
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
        public IVertexConfigurator FromVertex { get; private set; }
        public string FromEndpoint { get; private set; }

        public IVertexConfigurator ToVertex { get; private set; }
        public string ToEndpoint { get; private set; }

        public Edge(IVertexConfigurator fromVertex, string fromEndpoint, IVertexConfigurator toVertex, string toEndpoint)
        {
            FromVertex = fromVertex ?? throw new ArgumentNullException(nameof(fromVertex));
            FromEndpoint = fromEndpoint ?? throw new ArgumentNullException(nameof(fromEndpoint));

            ToVertex = toVertex ?? throw new ArgumentNullException(nameof(toVertex));
            ToEndpoint = toEndpoint ?? throw new ArgumentNullException(nameof(toEndpoint));

        }
    }
}