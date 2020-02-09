using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackSP.Interfaces.Serialization
{
    public interface IParallelSerializer
    {
        void StartSerialization<T>(Stream outputStream, T @event);
    }
}
