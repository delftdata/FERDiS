using BlackSP.Kernel.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Core.Operators.Concrete
{
    public class JoinOperator<TInA, TInB, TOut> : SlidingWindowedOperatorBase
        where TInA : class, IEvent
        where TInB : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IJoinOperatorConfiguration<TInA, TInB, TOut> _options;
        public JoinOperator(IJoinOperatorConfiguration<TInA, TInB, TOut> options) : base(options)
        {
            _options = options;

            //ensures both windows that we need have been created on time
            GetWindow(typeof(TInA));
            GetWindow(typeof(TInB));
        }

        protected override IEnumerable<IEvent> OperateWithUpdatedWindows(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            var isInputTypeA = @event.GetType().Equals(typeof(TInA));
            Console.WriteLine("GONNA TRY JOIN " + @event.Key);
            return isInputTypeA ? PerformJoinLogic(@event as TInA) : PerformJoinLogic(@event as TInB);
            
        }

        private IEnumerable<TOut> PerformJoinLogic(TInA targetEvent)
        {
            IEnumerable<TInB> windowBs = GetWindow(typeof(TInB)).Events.Cast<TInB>();

            var matches = windowBs.Where(wEvent => _options.Match(targetEvent, wEvent));
            foreach (var match in matches)
            {
                yield return _options.Join(targetEvent, match);
            }
        }

        private IEnumerable<TOut> PerformJoinLogic(TInB targetEvent)
        {
            IEnumerable<TInA> windowAs = GetWindow(typeof(TInA)).Events.Cast<TInA>();

            var matches = windowAs.Where(wEvent => _options.Match(wEvent, targetEvent));
            foreach (var match in matches)
            {
                yield return _options.Join(match, targetEvent);
            }
        }
    }
}
