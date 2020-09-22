using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Models
{
    /// <inheritdoc/>
    public class RecoveryLine : IRecoveryLine
    {
        public IEnumerable<string> AffectedWorkers => RecoveryMap.Where(kv => kv.Value != Guid.Empty).Select(kv => kv.Key);

        public IDictionary<string, Guid> RecoveryMap { get; private set; }

        public RecoveryLine(IDictionary<string, Guid> recoveryMap)
        {
            RecoveryMap = recoveryMap ?? throw new ArgumentNullException(nameof(recoveryMap));
        }
    }
}
