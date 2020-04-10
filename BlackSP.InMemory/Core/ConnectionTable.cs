using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace BlackSP.InMemory.Core
{
    public class ConnectionTable
    {

        public void RegisterConnection(Connection connection)
        {
            var fromEndpointKey = $"{connection.FromEndpointName}${connection.FromOperatorName}${connection.FromInstanceName}";
        }

        public ICollection<Stream> GetIncomingConnections(string operatorName, string instanceName, string endpointName)
        {
            ICollection<Stream> x = null;
            //x.get;
            return null;
        }
    }
}
