using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Interfaces.Endpoints;
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
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private readonly IOperatorConfiguration _options;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ICollection<IOutputEndpoint> _outputEndpoints;

        private Task _operatingThread;
        private bool _isRequestedToStop;

        /// <summary>
        /// Base constructor for Operators, will throw when passing null options
        /// </summary>
        /// <param name="options"></param>
        public BaseOperator(IOperatorConfiguration options)
        {
            InputQueue = new BlockingCollection<IEvent>();

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputEndpoints = new List<IOutputEndpoint>();
            _cancellationTokenSource = new CancellationTokenSource();
            _isRequestedToStop = false;
        }

        /// <summary>
        /// Starts the operating background thread<br/> 
        /// (take from input, invoke user function, put in all output queues)
        /// </summary>
        public virtual Task Start()
        {
            _isRequestedToStop = false;
            return _operatingThread = Task.Run(Operate);
        }

        /// <summary>
        /// Stops the operating background thread
        /// </summary>
        public virtual Task Stop()
        {
            if(_operatingThread == null)
            {
                throw new Exception("The operator cannot be stopped as it has not been started"); //TODO: custom exception
            }

            _isRequestedToStop = true;
            if(!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            return _operatingThread;
        }

        public void RegisterOutputEndpoint(IOutputEndpoint outputEndpoint)
        {
            _outputEndpoints.Add(outputEndpoint);
        }

        protected abstract IEnumerable<IEvent> OperateOnEvent(IEvent @event);

        /// <summary>
        /// Forwards provided output events to registered output endpoints</br>
        /// Protected to allow extending classes to also egress events from
        /// other channels than the primary working thread
        /// </summary>
        /// <param name="outputs"></param>
        /// <param name="outputMode"></param>
        protected void EgressOutputEvents(IEnumerable<IEvent> outputs, OutputMode outputMode = OutputMode.Partition)
        {
            lock(_outputEndpoints)
            {
                foreach (var endpoint in _outputEndpoints)
                {
                    endpoint.Enqueue(outputs, outputMode);
                }
            }
        }

        /// <summary>
        /// The core method that does the endless loop of taking in an event, applying an operation on it
        /// and forwarding it to output
        /// </summary>
        private void Operate()
        {
            try
            {
                var inputEnumerable = InputQueue.GetConsumingEnumerable(_cancellationTokenSource.Token);
                foreach (IEvent @event in inputEnumerable)
                {
                    var results = OperateOnEvent(@event) ?? throw new NullReferenceException("OperateOnEvent returned null instead of enumerable");
                    EgressOutputEvents(results);
                }
            } catch(Exception e)
            {
                //TODO: change for logging at fatal level?
                Console.WriteLine($"Exception in operating thread, proceeding to shut down. Exception:\n{e}");
                Stop();
                
                throw;
            }
            
        }
    }
}
