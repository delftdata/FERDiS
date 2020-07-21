using BlackSP.Checkpointing.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.UnitTests.Models
{
    public class ClassA
    {

        [Checkpointable]
        private string _value;


        public ClassA(string value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void Append(string value)
        {
            _value += value;
        }

        public string GetValue()
        {
            return _value;
        }
    }
}
