using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BlackSP.Interfaces.Operators;

namespace BlackSP.Core.Operators
{
    public class BaseOperator : IOperator
    {
        private Task deserializationThread;
        private Task operatingThread;
        private Task serializationThread;



        public BaseOperator()
        {

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

        //- each vertex pair should have their own endpoints connected
        //--- NO SHARED ENDPOINTS

        //THEREFORE: enqueue outgoing events in all output endpoints,
        //           endpoints will respectively handle partitioning among shards etc
    }
}
