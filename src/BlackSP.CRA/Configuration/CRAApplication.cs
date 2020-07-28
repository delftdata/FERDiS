using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Configuration
{
    /// <summary>
    /// Application that does not actually run on the machine the configuration happens on. It is basically an empty class;
    /// </summary>
    public class CRAApplication : IApplication
    {
        public void Run()
        {
            return;
        }

        public Task RunAsync()
        {
            return Task.CompletedTask;
        }
    }
}
