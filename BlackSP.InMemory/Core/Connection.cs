using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.InMemory.Core
{
    public class Connection
    {
        public string FromOperatorName { get; set; }
        public string FromInstanceName { get; set; }
        public string FromEndpointName { get; set; }

        public string ToOperatorName { get; set; }
        public string ToInstanceName { get; set; }
        public string ToEndpointName { get; set; }
    }
}
