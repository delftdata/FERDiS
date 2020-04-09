using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Core.OperatorSockets
{
    public class JoinOperatorSocket<TInA, TInB, TOut> : SlidingWindowedOperatorSocketBase
        where TInA : class, IEvent
        where TInB : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IJoinOperator<TInA, TInB, TOut> _pluggedInOperator;
        public JoinOperatorSocket(IJoinOperator<TInA, TInB, TOut> pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;

            //ensures both windows that we need have been created on time
            GetWindow(typeof(TInA));
            GetWindow(typeof(TInB));
        }

        protected override IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            var isInputTypeA = @event.GetType().Equals(typeof(TInA));
            //Console.WriteLine("GONNA TRY JOIN " + @event.Key);
            return isInputTypeA ? PerformJoinLogic(@event as TInA) : PerformJoinLogic(@event as TInB);
            
        }

        private IEnumerable<TOut> PerformJoinLogic(TInA targetEvent)
        {
            IEnumerable<TInB> windowBs = GetWindow(typeof(TInB)).Events.Cast<TInB>();

            var matches = windowBs.Where(wEvent => _pluggedInOperator.Match(targetEvent, wEvent));
            foreach (var match in matches)
            {
                yield return _pluggedInOperator.Join(targetEvent, match);
            }
        }

        private IEnumerable<TOut> PerformJoinLogic(TInB targetEvent)
        {
            IEnumerable<TInA> windowAs = GetWindow(typeof(TInA)).Events.Cast<TInA>();

            var matches = windowAs.Where(wEvent => _pluggedInOperator.Match(wEvent, targetEvent));
            foreach (var match in matches)
            {
                yield return _pluggedInOperator.Join(match, targetEvent);
            }
        }
    }
}
