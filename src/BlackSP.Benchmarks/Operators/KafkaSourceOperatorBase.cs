using BlackSP.Benchmarks.NEXMark;
using BlackSP.Checkpointing;
using BlackSP.Kernel.Models;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Benchmarks.Operators
{
    public abstract class KafkaSourceOperatorBase<T> : ICheckpointableAnnotated
        where T : class
    {
        protected readonly int PartitionCountPerTopic = 6;
        protected readonly int BrokerCount = 6;
        protected readonly string BrokerDomainNameTemplate = "localhost:3240{0}";//"kafka-{0}.kafka.kafka.svc.cluster.local:9092";

        protected abstract string TopicName { get; }
        protected IConsumer<int, T> Consumer { get; private set; }

        [Checkpointable]
        private IDictionary<int, int> _offsets;

        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        protected ILogger Logger => _logger;
        protected IVertexConfiguration VertexConfiguration => _vertexConfiguration;

        protected KafkaSourceOperatorBase(IVertexConfiguration vertexConfig, ILogger logger)
        {
            _vertexConfiguration = vertexConfig ?? throw new ArgumentNullException(nameof(vertexConfig));   
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _offsets = new Dictionary<int, int>();
            
            GetConsumer(); //ensure initialisation of Consumer property
        }

        public void OnBeforeRestore() { }

        public void OnAfterRestore() => GetConsumer(true);

        protected void UpdateOffsets(int tp, int offset)
        {
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
                Consumer = new ConsumerBuilder<int, T>(GetConsumerConfig(_vertexConfiguration.VertexName))
                    .SetValueDeserializer((new ProtoBufAsyncValueSerializer<T>() as IAsyncDeserializer<T>).AsSyncOverAsync())
                    .SetErrorHandler((_, e) => {
                        _logger.Error($"Error raised by Kafka consumer: {e}");
                        if (e.IsFatal) { throw new KafkaException(e); }
                    })
                    .Build();

                var assignedPartitions = GetPartitions();
                foreach(var partition in assignedPartitions)
                {
                    if(!_offsets.ContainsKey(partition))
                    {
                        _offsets.Add(partition, 0);
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
            var vertexShardId = _vertexConfiguration.ShardId;
            var vertexShardCount = _vertexConfiguration.InstanceNames.Count();
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
                //EnableAutoCommit = false,
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
