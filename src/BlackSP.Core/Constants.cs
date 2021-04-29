using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public static class Constants
    {

        public const int KeepAliveTimeoutSeconds = 16;
        public const int KeepAliveIntervalSeconds = 4;

        /// <summary>
        /// Provides a default value for the queue size of queues on thread boundaries
        /// </summary>
        public const int DefaultThreadBoundaryQueueSize = 1 << 4;

        /// <summary>
        /// When set to true will skip processor start checks
        /// </summary>
        public const bool SkipProcessorPreStartHooks = false;
    }
}
