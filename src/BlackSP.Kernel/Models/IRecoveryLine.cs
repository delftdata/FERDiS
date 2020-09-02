using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Models
{
    /// <summary>
    /// Model class holding recovery line information
    /// </summary>
    public interface IRecoveryLine
    {

        /// <summary>
        /// A list of all instance names that are affected if this recovery line is applied
        /// </summary>
        IEnumerable<string> AffectedWorkers { get; }

        /// <summary>
        /// Contains instance names as key and checkpoint identifiers as values<br/>
        /// An instance missing from this map does not require recovering a checkpoint
        /// </summary>
        IDictionary<string, Guid> RecoveryMap { get; }
    }
}
