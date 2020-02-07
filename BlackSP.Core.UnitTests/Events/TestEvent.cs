using BlackSP.Core.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.UnitTests.Events
{
    public struct TestEvent : IEvent {
        
        private int _value;
        public string Key { get; }

        public TestEvent(string key, int value)
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
