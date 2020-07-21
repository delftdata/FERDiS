using BlackSP.Checkpointing.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests.Models
{
    class ClassB
    {

        [Checkpointable]
        private int _counter;

        public ClassB()
        {
            _counter = 0;
        }

        public void IncrementCounter()
        {
            _counter++;
        }

        public int Counter => _counter;

    }
}
