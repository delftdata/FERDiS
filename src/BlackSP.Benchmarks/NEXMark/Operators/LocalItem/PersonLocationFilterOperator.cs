using BlackSP.Benchmarks.NEXMark.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Operators.LocalItem
{
    class PersonLocationFilterOperator : IFilterOperator<PersonEvent>
    {
        public PersonEvent Filter(PersonEvent @event)
        {
            var address = @event.Person.Address;
            return address != null && address.Province.Length > 0 && address.Province[0] % 2 == 0
                ? @event 
                : null;
        }
    }
}
