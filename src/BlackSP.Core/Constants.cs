using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core
{
    public static class Constants
    {

        public const int KeepAliveTimeoutSeconds = 1 << 16;
        public const int KeepAliveIntervalSeconds = 1 << 16;

        public const int HeartbeatSeconds = 15;

        /// <summary>
        /// Provides a default value for the queue size of queues on thread boundaries
        /// </summary>
        public const int DefaultThreadBoundaryQueueSize = 1 << 12;
    }
}
