using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    [ProtoContract]
    public class Address
    {
        [ProtoMember(1)]
        public string Street { get; set; }

        [ProtoMember(2)]
        public string City { get; set; }

        [ProtoMember(3)]
        public string Country { get; set; }

        [ProtoMember(4)]
        public string Province { get; set; }

        [ProtoMember(5)]
        public string Zipcode { get; set; }
    }
}
