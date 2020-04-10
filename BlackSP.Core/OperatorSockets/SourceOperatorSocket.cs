using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.OperatorSockets
{
    public class SourceOperatorSocket<TEvent> : OperatorSocketBase 
        where TEvent : class, IEvent
    {
        private readonly ISourceOperator<TEvent> _pluggedInOperator;

        public SourceOperatorSocket(ISourceOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        public override Task Start(DateTime at)
        {
            //TODO: start producing..?
            return base.Start(at);
        }

        /// <summary>
        /// This method will never be invoked, a source operator will never have an input endpoint.
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        protected override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            throw new NotImplementedException(); 
        }

        

    }
}
