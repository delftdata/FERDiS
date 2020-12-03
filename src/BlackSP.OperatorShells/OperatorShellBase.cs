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

        /// <summary>
        /// Base constructor for Operators
        /// </summary>
        public OperatorShellBase()
        {
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
