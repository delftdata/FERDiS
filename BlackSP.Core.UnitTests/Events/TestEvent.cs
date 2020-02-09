using BlackSP.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Events
{
    public class TestEvent : IEvent {
        
        private byte _value;
        public string Key { get; }

        public TestEvent(string key, byte value)
        {
            Key = key;
            _value = value;
        }


        public object GetValue()
        {
            return _value;
        }
    }
}
