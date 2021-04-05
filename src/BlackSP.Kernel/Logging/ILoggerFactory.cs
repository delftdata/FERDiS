using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Logging
{
    public interface ILoggerFactory
    {

        ILogger GetDefaultLogger();
        ILogger GetPerformanceLogger();
        ILogger GetCheckpointLogger();
        ILogger GetRecoveryLogger();
    }
}
