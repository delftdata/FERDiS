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
    /// Takes from any of the sources, passes to pipeline one at a time via thread synchronization and passes results to dispatcher
    /// </summary>
    public class ControlLayerProcessController : MultiSourceProcessControllerBase<ControlMessage>
    {
        public ControlLayerProcessController(
            IEnumerable<ISource<ControlMessage>> sources,
            IPipeline<ControlMessage> pipeline,
            IDispatcher<ControlMessage> dispatcher) : base(sources, pipeline, dispatcher) 
        {
        }
    }
}
