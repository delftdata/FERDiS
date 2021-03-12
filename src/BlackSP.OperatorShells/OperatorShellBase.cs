using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;

namespace BlackSP.OperatorShells
{
    public abstract class OperatorShellBase : IOperatorShell, IDisposable
    {
        private readonly IOperator _operator;
        private readonly ICycleOperator _cycleOperator;


        /// <summary>
        /// Base constructor for Operators
        /// </summary>
        public OperatorShellBase(IOperator @operator)
        {
            _operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
            _cycleOperator = @operator as ICycleOperator;
            
        }

        public async Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event, bool isCycleInput = false)
        {
            if(isCycleInput)
            {
                await (_cycleOperator?.Consume(@event) 
                    ?? throw new InvalidOperationException($"Tried to pass cycle input but operator of type {_operator.GetType()} does not implement the {typeof(ICycleOperator)} interface")
                    ).ConfigureAwait(false);
                return Enumerable.Empty<IEvent>();
            } 

            return await OperateOnEvent(@event).ConfigureAwait(false);
        }

        public abstract Task<IEnumerable<IEvent>> OperateOnEvent(IEvent @event);

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
