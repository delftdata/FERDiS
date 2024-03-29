using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public static class Constants
    {

        public const int KeepAliveTimeoutSeconds = 15;
        public const int KeepAliveIntervalSeconds = 1;

        /// <summary>
        /// Provides a default value for the queue size of queues on thread boundaries
        /// </summary>
        public const int DefaultThreadBoundaryQueueSize = 1 << 8;

        /// <summary>
        /// When set to true will skip processor start checks
        /// </summary>
        public const bool SkipProcessorPreStartHooks = false;
    }
}
