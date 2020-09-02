using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    /// <inheritdoc/>
    public class RecoveryLine : IRecoveryLine
    {
        public IEnumerable<string> AffectedWorkers => RecoveryMap.Keys;

        public IDictionary<string, Guid> RecoveryMap { get; private set; }

        public RecoveryLine(IDictionary<string, Guid> recoveryMap)
        {
            RecoveryMap = recoveryMap ?? throw new ArgumentNullException(nameof(recoveryMap));
        }
    }
}
