using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    [ProtoContract]
    [Serializable]
    public class Person
    {
        public static readonly string KafkaTopicName = "people";

        /// <summary>
        /// PK
        /// </summary>
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string FullName { get; set; }

        [ProtoMember(3)]
        public string Email { get; set; }

        [ProtoMember(4)]
        public string PhoneNumber { get; set; }

        [ProtoMember(5)]
        public string Website { get; set; }

        [ProtoMember(6)]
        public string CreditCard { get; set; }

        [ProtoMember(7)]
        public Profile Profile { get; set; }

        [ProtoMember(8)]
        public Address Address { get; set; }


    }
}
