using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Core.Operators.Concrete
{
    public class JoinOperator : BaseSlidingWindowedOperator
    {
        private readonly IJoinOperatorConfiguration _options;
        public JoinOperator(IJoinOperatorConfiguration options) : base(options)
        {
            _options = options;
        }

        protected override IEnumerable<IEvent> PreWindowInsert(IEvent @event)
        {
            var matches = _currentWindow.Where(pair => pair.Value.JoinKey == @event.JoinKey);
            foreach (var match in matches)
            {
                yield return _options.Join(@event, match.Value);
            }
        }
    }
}
