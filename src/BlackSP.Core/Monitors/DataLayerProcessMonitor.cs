using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Monitors
{
    public class DataLayerProcessMonitor
    {
        public bool IsActive { get; private set; }

        public DataLayerProcessMonitor()
        {
            IsActive = false;
        }

        public void MarkActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
