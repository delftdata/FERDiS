using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Interfaces.Serialization
{
    public interface ISerializer
    {
        /// <summary>
        /// Write serialized object to stream with 
        /// it's serialized length prefixed to it. <br/>
        /// This serves an important role for use in 
        /// network communication.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="event"></param>
        Task Serialize<T>(Stream outputStream, T obj);
        
        /// <summary>
        /// Read next T from the inputstream, expects
        /// the serialized Ts length as a prefixed int32
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        Task<T> Deserialize<T>(Stream inputStream, CancellationToken t);

    }
}
