using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Kernel.Serialization
{
    public interface IStreamSerializer
    {
        /// <summary>
        /// Serialize object to stream
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="event"></param>
        Task Serialize<T>(Stream outputStream, T obj);
        
        /// <summary>
        /// Read next T from the inputstream
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<T> Deserialize<T>(Stream inputStream, CancellationToken t);

    }
}
