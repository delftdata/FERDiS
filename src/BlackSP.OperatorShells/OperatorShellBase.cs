using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;

namespace BlackSP.OperatorShells
{
    public abstract class OperatorShellBase : IOperatorShell, IDisposable
    {
        private readonly IOperator _pluggedInOperator;


        /// <summary>
        /// Base constructor for Operators, will throw when passing null options
        /// </summary>
        /// <param name="options"></param>
        public OperatorShellBase(IOperator pluggedInOperator)
        {
            _pluggedInOperator = pluggedInOperator ?? throw new ArgumentNullException(nameof(pluggedInOperator));

        }

        public abstract IEnumerable<IEvent> OperateOnEvent(IEvent @event);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
