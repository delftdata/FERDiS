using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.Benchmarks.WordCount.Events
{
    /// <summary>
    /// serializable required as these events are aggregated 
    /// and may therefore be part of a checkpoint
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class WordEvent : IEvent
    {
        public int? Key { 
            get {
                return Word != null ? (int?)Word[0] : null;
            }
        }

        [ProtoMember(1)]
        public DateTime EventTime { get; set; }

        [ProtoMember(2)]
        public string Word { get; set; }

        [ProtoMember(3)]
        public int Count { get; set; }

        public WordEvent() { }

        [ProtoMember(4)]
        public int EC { get; set; }

        public int EventCount() => EC > 0 ? EC : 1;

    }
}
