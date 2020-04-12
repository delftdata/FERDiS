using BlackSP.Infrastructure.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.InMemory.Configuration
{
    public class IdentityTable
    {
        private readonly IDictionary<string, IHostParameter> _instanceParameters;
        
        public IdentityTable()
        {
            _instanceParameters = new Dictionary<string, IHostParameter>();
        }

        public void Add(string instanceName, IHostParameter parameter)
        {
            _instanceParameters.Add(instanceName, parameter);
            
        }

        public IHostParameter GetHostParameter(string instanceName)
        {
            return _instanceParameters[instanceName];
        }

        public ICollection<string> GetAllInstanceNames()
        {
            return _instanceParameters.Keys;
        }
    }
}
