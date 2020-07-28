using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Builders
{
    public interface IApplication
    {

        void Run();

        Task RunAsync();

    }
}
