using BlackSP.Infrastructure.Modules;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Builders.Vertex
{
    public class CoordinatorVertexBuilder : VertexBuilderBase
    {
        public override VertexType VertexType => VertexType.Coordinator;

        public override Type ModuleType => typeof(CoordinatorModule);

        public CoordinatorVertexBuilder(string[] instanceNames, string vertexName) : base(instanceNames, vertexName)
        {
        }
    }
}
