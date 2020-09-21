using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BlackSP.ThroughputExperiment.Events
{
    [ProtoContract]
    public class SampleEvent2 : IEvent
    {
        [ProtoMember(1)]
        public string Key { get; set; }

        [ProtoMember(2)]
        public DateTime EventTime { get; set; }

        [ProtoMember(3)]
        public int EventCount { get; set; }

        public SampleEvent2() { }

        public SampleEvent2(string key, DateTime? eventTime, int count)
        {
            Key = key;
            EventTime = eventTime ?? throw new ArgumentNullException(nameof(eventTime));
            EventCount = count;
        }

        public int GetPartitionKey()
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(Key));
            return BitConverter.ToInt32(hashed, 0);
            //return Key.GetHashCode();
        }
    }
}
