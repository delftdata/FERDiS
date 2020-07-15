using BlackSP.Infrastructure;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Simulator.Configuration
{
    public class IdentityTable
    {
        private readonly IDictionary<string, IHostConfiguration> _instanceParameters;
        
        public IdentityTable()
        {
            _instanceParameters = new Dictionary<string, IHostConfiguration>();
        }

        public void Add(string instanceName, IHostConfiguration parameter)
        {
            _instanceParameters.Add(instanceName, parameter);
            
        }

        public IHostConfiguration GetHostConfiguration(string instanceName)
        {
            return _instanceParameters[instanceName];
        }

        public ICollection<string> GetAllInstanceNames()
        {
            return _instanceParameters.Keys;
        }
    }
}
