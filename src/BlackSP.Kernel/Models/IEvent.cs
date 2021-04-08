using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IEvent
    {
        int? Key { get; }
        
        DateTime EventTime { get; }

        //int Count => 1;
    }
}
