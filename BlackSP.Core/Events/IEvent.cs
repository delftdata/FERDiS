using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Events
{
    public interface IEvent
    {
        string Key { get; }

        object GetValue();
    }
}
