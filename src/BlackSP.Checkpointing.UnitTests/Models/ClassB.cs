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

        [Checkpointable]
        private byte[] _largeBoi;

        public ClassB()
        {
            _counter = 0;
        }

        public void IncrementCounter()
        {
            _counter++;
            
        }

        public int Counter => _counter;

        public int GetLargeArraySize()
        {
            return _largeBoi.Length;
        }

        public void SetLargeArraySize(int size)
        {
            _largeBoi = new byte[size];
        }
    }
}
