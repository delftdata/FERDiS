using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.Benchmarks.WordCount.Events
{
    [ProtoContract]
    [Serializable]
    public class SentenceEvent : IEvent
    {
        public int? Key { 
            get {
                MD5 md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(Sentence));
                return BitConverter.ToInt32(hashed, 0);
            } 
        }

        [ProtoMember(1)]
        public DateTime EventTime { get; set; }

        [ProtoMember(2)]
        public string Sentence { get; set; }

        public SentenceEvent() { }
    }
}
