using BlackSP.Core.Endpoints;
using BlackSP.Core.Serialization;
using BlackSP.Core.Serialization.Parallelization;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class TestOutputEndpoint : BaseOutputEndpoint
    {
        public TestOutputEndpoint(IParallelEventSerializer serializer) : base(serializer)
        {
            
        }
    }
}
