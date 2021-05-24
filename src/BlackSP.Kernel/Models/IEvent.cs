using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IEvent
    {
        int? Key { get; }
        
        DateTime EventTime { get; }

        /// <summary>
        /// How many events shaped the current event<br/>
        /// Defaults to 1 but can be overridden for Aggregate operations
        /// </summary>
        public int EventCount() => 1;
    }
}
