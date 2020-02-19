using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Core.Streams;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;

namespace BlackSP.Core.Endpoints
{
    public class BaseInputEndpoint : IInputEndpoint
    {
        protected ConcurrentQueue<IEvent> _inputQueue;
        private ISerializer _serializer;

        public BaseInputEndpoint(ISerializer serializer)
        {
            _inputQueue = new ConcurrentQueue<IEvent>();
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
            //stopwatch and counter for test purposes
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double counter = 0;
            while (!t.IsCancellationRequested)
            {
                int nextMsgLength = await s.ReadInt32Async();
                byte[] buffer = new byte[nextMsgLength]; //TODO: swap for arraypool?
                int realMsgLength = await s.ReadAllRequiredBytesAsync(buffer, 0, nextMsgLength);
                if(nextMsgLength != realMsgLength)
                {
                    //TODO: log/throw?
                }
                //TODO: enqueue buffer

                //TODO: remove event stuff below
                var nextEvent = await _serializer.Deserialize<IEvent>(s, t);
                if(nextEvent != null)
                {
                    _inputQueue.Enqueue(nextEvent);
                    
                    //stopwatch and counter for test purposes
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
