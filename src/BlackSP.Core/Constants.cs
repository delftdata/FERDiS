using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public static class Constants
    {

        public const int KeepAliveTimeoutSeconds = 15;
        public const int KeepAliveIntervalSeconds = 5;

        public const int HeartbeatSeconds = 30;

        /// <summary>
        /// Provides a default value for the queue size of queues on thread boundaries
        /// </summary>
        public const int DefaultThreadBoundaryQueueSize = 1 << 12;

        /// <summary>
        /// TODO
        /// </summary>
        public const int MetricIntervalSeconds = 10;

        /// <summary>
        /// When set to true will skip processor start checks
        /// </summary>
        public const bool SkipProcessorPreStartHooks = false;
    }
}
