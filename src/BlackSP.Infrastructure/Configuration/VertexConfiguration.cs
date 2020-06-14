using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Configuration
{
    public class VertexConfiguration : IVertexConfiguration
    {
        public string OperatorName { get; set; }

        public string InstanceName { get; set; }

        public VertexType VertexType { get; set; }

        public ICollection<IEndpointConfiguration> InputEndpoints { get; set; }

        public ICollection<IEndpointConfiguration> OutputEndpoints { get; set; }

    }
}
