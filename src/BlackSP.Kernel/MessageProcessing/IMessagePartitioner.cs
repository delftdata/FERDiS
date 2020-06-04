﻿using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel
{
    public interface IMessagePartitioner
    {
        IEnumerable<string> Partition(IMessage message);

        string GetEndpointKey(string endpointName, int shardId);
    }
}
