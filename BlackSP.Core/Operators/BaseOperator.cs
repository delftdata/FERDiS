using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
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
        //todo create enough outputqueues based on options in ctor
        public BlockingCollection<IEvent> OutputQueue { get; private set; }


        private Task deserializationThread;
        private Task operatingThread;
        private Task serializationThread;

        /// <summary>
        /// Base constructor for Operators, will throw when passing null options
        /// </summary>
        /// <param name="options"></param>
        public BaseOperator(IOperatorConfiguration options)
        {
            var config = options ?? throw new ArgumentNullException(nameof(options));
            
            //TODO: instantiate output queues
            var outputEndpointCount = config.OutputEndpointCount ?? throw new ArgumentNullException(nameof(config.OutputEndpointCount));
            //...
            InputQueue = new BlockingCollection<IEvent>();

        }


        /// <summary>
        /// Starts the deserialization, operating and serialization threads
        /// </summary>
        public void Start()
        {


            //.ContinueWith(task => {
            //     Console.WriteLine(task.Exception);
            //     return 0;
            // }, TaskContinuationOptions.OnlyOnFaulted)
        }

        //protected 

        
    }
}
