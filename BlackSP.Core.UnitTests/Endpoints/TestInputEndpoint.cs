using BlackSP.Core.Endpoints;
using BlackSP.Interfaces.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Endpoints
{
    public class TestInputEndpoint : BaseInputEndpoint
    {
        public TestInputEndpoint(ISerializer serializer) : base(serializer)
        {
            
        }
    }
}
