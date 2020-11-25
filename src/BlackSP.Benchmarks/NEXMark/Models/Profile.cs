using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.NEXMark.Models
{
    public class Profile
    {
        /// <summary>
        /// Contains a collection of CategoryIds
        /// </summary>
        public IEnumerable<int> Interests { get; set; }

        /// <summary>
        /// Amount of currency
        /// </summary>
        public double Income { get; set; }

        public bool IsBusiness { get; set; }

        public int? Age { get; set; }

        /// <summary>
        /// either "male", "female" or null if unknown
        /// </summary>
        public string Gender { get; set; }

        public string Education { get; set; }
    }

}
