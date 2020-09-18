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
    /// Takes from any of the sources, passes to pipeline one at a time via thread synchronization and passes results to dispatcher
    /// </summary>
    public class ControlMessageProcessor : MultiSourceProcessorBase<ControlMessage>
    {
        public ControlMessageProcessor(
            IEnumerable<ISource<ControlMessage>> sources,
            IPipeline<ControlMessage> pipeline,
            IDispatcher<ControlMessage> dispatcher) : base(sources, pipeline, dispatcher) 
        {
        }
    }
}
