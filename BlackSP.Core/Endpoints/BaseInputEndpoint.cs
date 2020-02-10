using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;

namespace BlackSP.Core.Endpoints
{
    public class BaseInputEndpoint<T> : IInputEndpoint<T> where T : class, IEvent
    {
        protected ConcurrentQueue<T> _inputQueue;
        private ISerializer _serializer;

        public BaseInputEndpoint(ISerializer serializer)
        {
            _inputQueue = new ConcurrentQueue<T>();
            _serializer = serializer;// new ApexSerializer<IEvent>(Binary.Create());
        }

        /// <summary>
        /// Starts reading from the inputstream and storing results in local inputqueue.
        /// This method will block execution, ensure it is running on a background thread.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public async Task Ingress(Stream s, CancellationToken t)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double counter = 0;
            while (!t.IsCancellationRequested)
            {
                var nextEvent = await _serializer.Deserialize<T>(s, t);
                if(nextEvent != null)
                {   
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

        public T GetNext()
        {
            return _inputQueue.TryDequeue(out T result) ? result : null;
        }

    }
}
