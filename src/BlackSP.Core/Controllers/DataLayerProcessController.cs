using BlackSP.Core.Controllers;
using BlackSP.Core.Models;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Controllers
{
    /// <summary>
    /// Controls the message passing between underlying components<br/>
    /// Takes data messages from the source, passes to pipeline one at a time and passes results to dispatcher
    /// </summary>
    public class DataLayerProcessController : SingleSourceProcessControllerBase<DataMessage>
    {
        public DataLayerProcessController(
            ISource<DataMessage> source,
            IPipeline<DataMessage> pipeline,
            IDispatcher<DataMessage> dispatcher) : base(source, pipeline, dispatcher)
        {
        }
    }
}
