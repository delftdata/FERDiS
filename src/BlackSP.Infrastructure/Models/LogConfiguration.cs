using BlackSP.Kernel.Configuration;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Models
{
    [Serializable]
    public class LogConfiguration : ILogConfiguration
    {
        public LogTargetFlags TargetFlags { get; set; }

        public LogEventLevel EventLevel { get; set; }

        /// <summary>
        /// Default configuration, targets console only at information level
        /// </summary>
        public LogConfiguration()
        {
            TargetFlags = LogTargetFlags.Console;
            EventLevel = LogEventLevel.Information;
        }

        /// <summary>
        /// Specifies specific log configuration
        /// </summary>
        /// <param name="targetFlags"></param>
        /// <param name="eventLevel"></param>
        public LogConfiguration(LogTargetFlags targetFlags, LogEventLevel eventLevel)
        {
            TargetFlags = targetFlags;
            EventLevel = eventLevel;
        }
    }
}
