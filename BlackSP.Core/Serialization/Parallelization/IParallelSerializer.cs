using BlackSP.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackSP.Core.Serialization.Parallelization
{
    public interface IParallelEventSerializer
    {
        void StartSerialization(Stream outputStream, IEvent @event);
    }
}
