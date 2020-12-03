using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.Core.Models
{
    [Serializable]
    public abstract class MD5PartitionKeyEventBase : IEvent
    {
        public abstract string Key { get; set; }
        public abstract DateTime EventTime { get; set; }

        public int GetPartitionKey()
        {
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms - no security risk, this is used for partitioning
            using MD5 md5Hasher = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
            return BitConverter.ToInt32(md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(Key)), 0);
        }
    }
}
