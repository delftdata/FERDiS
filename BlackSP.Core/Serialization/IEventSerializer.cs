using BlackSP.Core.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BlackSP.Core.Serialization
{
    public interface IEventSerializer
    {
        /// <summary>
        /// Write serialized event to stream
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="event"></param>
        void SerializeEvent(Stream outputStream, IEvent @event);
        
        /// <summary>
        /// Read next event from the inputstream
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        IEvent DeserializeEvent(Stream inputStream, CancellationToken t);

    }
}
