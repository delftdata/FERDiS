using BlackSP.Core.Endpoints;
using BlackSP.Interfaces.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class TestOutputEndpoint : BaseOutputEndpoint
    {
        public TestOutputEndpoint(ISerializer serializer) : base(serializer)
        {
            
        }
    }
}
