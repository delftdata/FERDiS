using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.OperatorShells
{
    public class SourceOperatorShell<TEvent> : OperatorShellBase 
        where TEvent : class, IEvent
    {
        private readonly ISourceOperator<TEvent> _pluggedInOperator;

        public SourceOperatorShell(ISourceOperator<TEvent> pluggedInOperator) : base()
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));
        }

        /// <summary>
        /// This method will never be invoked, a source operator will never have an input endpoint.
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            throw new NotImplementedException(); 
        }

        

    }
}
