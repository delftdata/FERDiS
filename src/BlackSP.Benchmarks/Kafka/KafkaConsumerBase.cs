using BlackSP.Checkpointing;
using BlackSP.Kernel.Configuration;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.Kafka
{
    public abstract class KafkaConsumerBase<T> : ICheckpointable
        where T : class
    {
        protected readonly int PartitionCountPerTopic = 6;
        protected readonly int BrokerCount = 6;
        protected readonly string BrokerDomainNameTemplate;

        protected abstract string TopicName { get; }
        protected IConsumer<int, T> Consumer { get; private set; }

        [ApplicationState]
        private IDictionary<int, int> _offsets;

        protected ILogger Logger { get; }
        protected IVertexConfiguration VertexConfiguration { get; }

        protected KafkaConsumerBase(IVertexConfiguration vertexConfig, ILogger logger)
        {
            VertexConfiguration = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));   
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _offsets = new Dictionary<int, int>();

            BrokerDomainNameTemplate = Environment.GetEnvironmentVariable("KAFKA_BROKER_DNS_TEMPLATE");

            GetConsumer(); //ensure initialisation of Consumer property
        }

        public void OnBeforeRestore() { }

        public void OnAfterRestore() => GetConsumer(true);

        protected void UpdateOffsets(int tp, int offset)
        {
            if(offset < 0) { offset = 0; }
            _offsets[tp] = offset;
        }

        protected IConsumer<int, T> GetConsumer(bool forceCreate = false)
        {
            if (forceCreate && Consumer != null)
            {
                Consumer.Dispose();
                Consumer = null;
            }

            if(Consumer == null)
            {
                Consumer = new ConsumerBuilder<int, T>(GetConsumerConfig(VertexConfiguration.VertexName+"wtf"))
                    .SetValueDeserializer((new ProtoBufAsyncValueSerializer<T>() as IAsyncDeserializer<T>).AsSyncOverAsync())
                    .SetErrorHandler((_, e) => {
                        if (e.IsFatal) {
                            Logger.Fatal($"Fatal error raised by Kafka consumer: {e}");
                            throw new KafkaException(e);
                        } else
                        {
                            Logger.Warning($"Error raised by Kafka consumer: {e}");
                        }
                    })
                    .Build();

                var assignedPartitions = GetPartitions();
                foreach(var partition in assignedPartitions)
                {
                    if(!_offsets.ContainsKey(partition))
                    {
                        _offsets.Add(partition, (int)Offset.Beginning);
                    }
                }
                var tpos = assignedPartitions
                    .Select(p => new TopicPartition(TopicName, p))
                    .Select(tp => new TopicPartitionOffset(tp, _offsets[tp.Partition]));
                Consumer.Assign(tpos);
            }
            return Consumer;
        }

        /// <summary>
        /// Determines which kafka topic partitions are assigned to the current instance
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Partition> GetPartitions()
        {
            var vertexShardId = VertexConfiguration.ShardId;
            var vertexShardCount = VertexConfiguration.InstanceNames.Count();
            var kafkaShardCount = PartitionCountPerTopic;
            for(int kafkaShard = 0; kafkaShard < kafkaShardCount; kafkaShard++)
            {
                //round-robin assignment of kafka-shards
                if(kafkaShard % vertexShardCount == vertexShardId)
                {
                    yield return kafkaShard;
                }
            }
        }

        private ConsumerConfig GetConsumerConfig(string groupId)
        {
            return new ConsumerConfig
            {
                BootstrapServers = GetBrokerList(),
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        private string GetBrokerList()
        {
            int i = 0;
            StringBuilder brokerList = new StringBuilder();
            while (i < BrokerCount)
            {
                brokerList.Append(string.Format(BrokerDomainNameTemplate, i));
                i++;
                if(i < BrokerCount)
                {
                    brokerList.Append(",");
                }
            }
            return brokerList.ToString();
        }
    }
}
