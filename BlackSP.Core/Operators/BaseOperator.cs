using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Operators;

namespace BlackSP.Core.Operators
{
    //- each operator pair will have their own endpoints connected --> not shared among operators
    //- options should specify how many output endpoints there are..
    //- so operator can just enqueue outgoing events in all output queues
    //- endpoints will write to input or read from assigned output queue
    //      endpoints will respectively handle partitioning among shards etc
    public abstract class BaseOperator : IOperator
    {
        public BlockingCollection<IEvent> InputQueue { get; private set; }
        public ConcurrentDictionary<int, BlockingCollection<IEvent>> OutputQueues { get; private set; }

        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isRequestedToStop;

        /// <summary>
        /// Base constructor for Operators, will throw when passing null options
        /// </summary>
        /// <param name="options"></param>
        public BaseOperator(IOperatorConfiguration options)
        {
            var config = options ?? throw new ArgumentNullException(nameof(options));
            var outputEndpointCount = config.OutputEndpointCount ?? throw new ArgumentNullException(nameof(config.OutputEndpointCount));            

            InputQueue = new BlockingCollection<IEvent>();
            OutputQueues = BuildOutputQueueDict(outputEndpointCount);

            _cancellationTokenSource = new CancellationTokenSource();
            _isRequestedToStop = false;
        }

        /// <summary>
        /// Starts the operating background thread 
        /// (take from input, invoke user function, put in all output queues)
        /// </summary>
        public virtual void Start()
        {
            _isRequestedToStop = false;
            Task.Run(StartOperating)
                .ContinueWith(HandleOperatingThreadException, TaskContinuationOptions.OnlyOnFaulted)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Stops the operating background thread
        /// </summary>
        public virtual void Stop()
        {
            _isRequestedToStop = true;
            if(!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        protected abstract IEnumerable<IEvent> OperateOnEvent(IEvent @event);

        private void StartOperating()
        {
            var inputEnumerable = InputQueue.GetConsumingEnumerable(_cancellationTokenSource.Token);
            foreach (IEvent @event in inputEnumerable)
            {
                OperateOnEvent(@event);
            }
        }

        private void HandleOperatingThreadException(Task operatingThread)
        {
            if (_isRequestedToStop)
            {
                return;
            }
            //TODO: change for logging at fatal level?
            Console.WriteLine($"Exception in operating thread, proceeding to shut down. Exception:\n{operatingThread.Exception}");
            Stop();
        }

        private ConcurrentDictionary<int, BlockingCollection<IEvent>> BuildOutputQueueDict(int queueCount)
        {
            bool dictInitOk = true;
            var outputQueueDict = new ConcurrentDictionary<int, BlockingCollection<IEvent>>();
            for (int i = 0; i < queueCount; i++)
            {
                dictInitOk = dictInitOk && outputQueueDict.TryAdd(i, new BlockingCollection<IEvent>());
            }
            if (!dictInitOk)
            {   //TODO: change for custom exception
                throw new Exception("OutputQueue collection initialization failed");
            }
            return outputQueueDict;
        }

    }
}
