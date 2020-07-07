using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Monitors
{
    public class DataProcessMonitor
    {
        public bool IsActive { get; private set; }

        public DataProcessMonitor()
        {
            IsActive = false;
        }

        public void MarkActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
