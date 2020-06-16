using BlackSP.Kernel.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.Endpoints;
using System;
using System.Collections.Generic;
using System.Linq;
using BlackSP.Infrastructure.Models;

namespace BlackSP.Infrastructure.Configuration
{
    public abstract class OperatorConfiguratorBase : VertexConfiguratorBase, IOperatorConfigurator
    {

        public override VertexType VertexType => VertexType.Operator;

        public OperatorConfiguratorBase(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        {
        }
    }
}
