using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.WordCount.Events
{
    [ProtoContract]
    public class WordEvent : IEvent
    {
        public string Key => Word;

        [ProtoMember(1)]
        public DateTime EventTime { get; set; }

        [ProtoMember(2)]
        public string Word { get; set; }

        [ProtoMember(3)]
        public int Count { get; set; }

        public WordEvent() { }

        public int GetPartitionKey()
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(Key));
            return BitConverter.ToInt32(hashed, 0);
        }
    }
}
