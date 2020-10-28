using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.StreamBench.WordCount.Events
{
    [ProtoContract]
    public class SentenceEvent : IEvent
    {
        public string Key => Sentence;

        [ProtoMember(1)]
        public DateTime EventTime { get; set; }

        [ProtoMember(2)]
        public string Sentence { get; set; }

        public SentenceEvent() { }

        public int GetPartitionKey()
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(Key));
            return BitConverter.ToInt32(hashed, 0);
        }
    }
}
