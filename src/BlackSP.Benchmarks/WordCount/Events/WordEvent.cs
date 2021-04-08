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
                MD5 md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(Word));
                return BitConverter.ToInt32(hashed, 0);
            }
        }

        [ProtoMember(1)]
        public DateTime EventTime { get; set; }

        [ProtoMember(2)]
        public string Word { get; set; }

        [ProtoMember(3)]
        public int Count { get; set; }

        public WordEvent() { }

    }
}
