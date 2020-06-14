using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    public interface IMessage
    {
        bool IsControl { get; }

        int PartitionKey { get; }
    }
}
