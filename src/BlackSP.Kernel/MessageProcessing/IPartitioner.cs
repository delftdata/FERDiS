using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel
{
    public interface IPartitioner
    {
        IEnumerable<string> Partition(IMessage message);
    }
}
