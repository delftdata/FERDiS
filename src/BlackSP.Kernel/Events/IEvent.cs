using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Events
{
    public interface IEvent
    {
        string Key { get; }

        DateTime EventTime { get; }

        int GetPartitionKey();
    }
}
