using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.OperatorShells
{
    public class SourceOperatorShell<TEvent> : OperatorShellBase 
        where TEvent : class, IEvent
    {
        private readonly ISourceOperator<TEvent> _pluggedInOperator;

        public SourceOperatorShell(ISourceOperator<TEvent> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;
        }

        //TODO: consider how to spontaneously emit events

        /*public override Task Start(DateTime at)
        {
            //var t =  base.Start(at);
            var t2 = Task.Run(async () =>
            {
                Console.WriteLine("Starting in 10 seconds");
                await Task.Delay(4 * 2500);
                while(!CancellationToken.IsCancellationRequested)
                {
                    EgressOutputEvents(_pluggedInOperator.GetTestEvents());
                    await Task.Delay(1).ConfigureAwait(false);
                }

                CancellationToken.ThrowIfCancellationRequested();
            });
            return Task.WhenAny(t,t2);
        }*/

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
