using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    public class Person
    {
        public static readonly string KafkaTopicName = "people";

        /// <summary>
        /// PK
        /// </summary>
        public int Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Website { get; set; }

        public string CreditCard { get; set; }

        public Profile Profile { get; set; }

        public Address Address { get; set; }


    }
}
