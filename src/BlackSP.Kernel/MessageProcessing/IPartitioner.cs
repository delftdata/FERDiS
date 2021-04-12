using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel
{
    public interface IPartitioner<T>
    {
        IEnumerable<(IEndpointConfiguration, int)> Partition(T message);
    }
}
