using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{

    [ProtoContract]
    public class Profile
    {
        /// <summary>
        /// Contains a collection of CategoryIds
        /// </summary>
        [ProtoMember(1)]
        public IEnumerable<int> Interests { get; set; }

        /// <summary>
        /// Amount of currency
        /// </summary>
        [ProtoMember(2)]
        public double Income { get; set; }

        [ProtoMember(3)]
        public bool IsBusiness { get; set; }

        [ProtoMember(4)]
        public int? Age { get; set; }

        /// <summary>
        /// either "male", "female" or null if unknown
        /// </summary>
        [ProtoMember(5)]
        public string Gender { get; set; }

        [ProtoMember(6)]
        public string Education { get; set; }
    }

}
