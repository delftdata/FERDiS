using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests.Models
{
    //a class that does not implement any checkpointable fields
    class ClassZ
    {
        private readonly string _wow;
        private readonly int _nice;
        private readonly BlockingCollection<int> _ints;

        public ClassZ()
        {
            _wow = "wow";
            _nice = 420;
            _ints = new BlockingCollection<int>();
        }

    }
}
