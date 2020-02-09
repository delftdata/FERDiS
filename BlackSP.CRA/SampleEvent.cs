using BlackSP.Core.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA
{
    public class SampleEvent : IEvent
    {
        private string _key;
        private string _value;

        public string Key => _key;

        public SampleEvent(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public object GetValue()
        {
            return _value;
        }
    }
}
