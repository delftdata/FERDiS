using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Processors
{
    /// <summary>
    /// Controls the message passing between underlying components<br/>
    /// Takes data messages from the source, passes to pipeline one at a time and passes results to dispatcher
    /// </summary>
    public class DataMessageProcessor : SingleSourceProcessorBase<DataMessage>
    {
        public DataMessageProcessor(
            ISource<DataMessage> source,
            IPipeline<DataMessage> pipeline,
            IDispatcher<DataMessage> dispatcher) : base(source, pipeline, dispatcher)
        {
        }


    }
}
