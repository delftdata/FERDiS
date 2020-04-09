﻿using BlackSP.Core.Windows;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Core.OperatorSockets
{
    public abstract class WindowedOperatorSocketBase<TIn, TOut> : OperatorSocketBase
        where TIn : class, IEvent
        where TOut : class, IEvent
    {

        private readonly IWindowedOperator _pluggedInOperator;
        private FixedEventWindow<TIn> _currentWindow;
        
        public WindowedOperatorSocketBase(IWindowedOperator pluggedInOperator) : base(pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator;            
        }

        public override Task Start(DateTime at)
        {
            _currentWindow = new FixedEventWindow<TIn>(at, _pluggedInOperator.WindowSize);
            return base.Start(at);
        }

        protected sealed override IEnumerable<IEvent> OperateOnEvent(IEvent @event)
        {
            _ = @event ?? throw new ArgumentNullException(nameof(@event));
            var typedEvent = @event as TIn ?? throw new ArgumentException($"Argument {nameof(@event)} was of type {@event.GetType()}, expected: {typeof(TIn)}");

            var closedWindow = _currentWindow.Insert(typedEvent);
            return !closedWindow.Any() ? Enumerable.Empty<IEvent>() : ProcessClosedWindow(closedWindow)
                ?? throw new Exception("ProcessClosedWindow returned null, expected IEnumerable");
        }

        protected abstract IEnumerable<TOut> ProcessClosedWindow(IEnumerable<TIn> closedWindow);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //_windowTimer?.Dispose();
                }
                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}