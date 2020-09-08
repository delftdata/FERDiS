using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure
{
    [Flags]
    public enum LogTargetFlags
    {
        None = 0,
        Console = 1 << 0,
        File = 1 << 1,
        AzureBlob = 1 << 2
    }

    /// <summary>
    /// Contains configuration for logging
    /// </summary>
    public interface ILogConfiguration
    {
        LogTargetFlags TargetFlags { get; }

        LogEventLevel EventLevel { get; }
    }
}
