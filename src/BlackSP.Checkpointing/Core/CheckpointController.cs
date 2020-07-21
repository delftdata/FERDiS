using BlackSP.Checkpointing.Extensions;
using BlackSP.Serialization.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BlackSP.Checkpointing.Core
{

    /// <summary>
    /// Controls creation and restoration of checkpoints
    /// </summary>
    class CheckpointController
    {

        private readonly ObjectRegistry _register;

        public CheckpointController(ObjectRegistry register)
        {
            _register = register ?? throw new ArgumentNullException(nameof(register));
        }

        

    }
}
