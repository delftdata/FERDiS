using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Core.Operators.Concrete
{
    public class JoinOperator<TInA, TInB, TOut> : BaseSlidingWindowedOperator
        where TInA : class, IEvent
        where TInB : class, IEvent
        where TOut : class, IEvent
    {
        private readonly IJoinOperatorConfiguration<TInA, TInB, TOut> _options;
        public JoinOperator(IJoinOperatorConfiguration<TInA, TInB, TOut> options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> PreWindowInsert(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));

            var isInputTypeA = @event.GetType().Equals(typeof(TInA));
            return isInputTypeA ? PerformJoinLogic(@event as TInA) : PerformJoinLogic(@event as TInB);
            
        }

        private IEnumerable<TOut> PerformJoinLogic(TInA targetEvent)
        {
            IEnumerable<TInB> windowBs = GetWindow(typeof(TInB)).Events as IEnumerable<TInB>;

            var matches = windowBs.Where(wEvent => _options.Match(targetEvent, wEvent));
            foreach (var match in matches)
            {
                yield return _options.Join(targetEvent, match);
            }
        }

        private IEnumerable<TOut> PerformJoinLogic(TInB targetEvent)
        {
            IEnumerable<TInA> windowAs = GetWindow(typeof(TInA)).Events as IEnumerable<TInA>;

            var matches = windowAs.Where(wEvent => _options.Match(wEvent, targetEvent));
            foreach (var match in matches)
            {
                yield return _options.Join(match, targetEvent);
            }
        }
    }
}
