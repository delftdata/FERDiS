using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apex.Serialization;
using BlackSP.Core.Events;
using BlackSP.Core.Serialization;

namespace BlackSP.Core.Endpoints
{
    public class BaseInputEndpoint : IInputEndpoint
    {
        protected ConcurrentQueue<IEvent> _inputQueue;
        private IEventSerializer _serializer;

        public BaseInputEndpoint()
        {
            _inputQueue = new ConcurrentQueue<IEvent>();
            _serializer = new ApexEventSerializer(Binary.Create());
        }

        /// <summary>
        /// Starts reading from the inputstream and storing results in local inputqueue.
        /// This method will block execution, ensure it is running on a background thread.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void Ingress(Stream s, CancellationToken t)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double counter = 0;
            while (!t.IsCancellationRequested)
            {
                var nextEvent = _serializer.DeserializeEvent(s, t);
                if(nextEvent != null)
                {   
                    //TODO BRUH JUST IGNORE FOR NOW DUE TO MEMORY ISSUES
                    _inputQueue.Enqueue(nextEvent);
                    counter++;
                    if(sw.ElapsedMilliseconds >= 10000)
                    {
                        double elapsedSeconds = (int)(sw.ElapsedMilliseconds / 1000d);
                        Console.WriteLine($"Ingressing {(int)(counter / elapsedSeconds)} e/s | {counter}/{elapsedSeconds}");
                        sw.Restart();
                        counter = 0;
                    }
                }
                
            }
        }

        public bool HasInput()
        {
            return !_inputQueue.IsEmpty;
        }

        public IEvent GetNext()
        {
            return _inputQueue.TryDequeue(out IEvent result) ? result : null;
        }

    }
}
