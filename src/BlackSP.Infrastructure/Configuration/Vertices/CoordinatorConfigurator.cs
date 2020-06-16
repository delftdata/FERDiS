using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Configuration.Vertices
{
    public class CoordinatorConfigurator : VertexConfiguratorBase
    {
        public override VertexType VertexType => VertexType.Coordinator;

        public override Type ModuleType => typeof(CoordinatorModule);

        public CoordinatorConfigurator(string[] instanceNames, string vertexName) : base(instanceNames, vertexName)
        {
        }
    }
}
