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
                return Sentence != null ? (int?)Sentence[0] : null;
            } 
        }

        [ProtoMember(1)]
        public DateTime EventTime { get; set; }

        [ProtoMember(2)]
        public string Sentence { get; set; }

        public SentenceEvent() { }
    }
}
