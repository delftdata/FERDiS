using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.PageRank.Models;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.PageRank
{
    /// <summary>
    /// Produces Pagerank data from a provided file containing edge (int, int) pairs
    /// </summary>
    public class Producer
    {

        public static async Task StartProductingGraphData(string brokerList, string edgeFileLocation)
        {
            _ = brokerList ?? throw new ArgumentNullException(nameof(brokerList));
            _ = edgeFileLocation ?? throw new ArgumentNullException(nameof(edgeFileLocation));

            var config = new ProducerConfig
            {
                BootstrapServers = brokerList,
                Partitioner = Partitioner.Consistent
            };

            
            using var adjacencyProducer = new ProducerBuilder<int, Adjacency>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Adjacency>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Adjacency stream error: {err}"))
                .Build();
            
            using StreamReader reader = File.OpenText(edgeFileLocation);

            string row = string.Empty;
            int currentFromId = 0;
            List<int> neighbours = new List<int>();

            while ((row = reader.ReadLine()) != null)
            {
                var ids = row.Split('\t');
                var fromId = int.Parse(ids[0]);
                var toId = int.Parse(ids[1]);

                if(fromId != currentFromId)
                {
                    var adjacency = new Adjacency { PageId = currentFromId, Neighbours = neighbours.ToArray() };
                    var message = new Message<int, Adjacency> { Key = adjacency.PageId, Value = adjacency };
                    await adjacencyProducer.ProduceAsync(Adjacency.KafkaTopicName, message);

                    currentFromId = fromId;
                    neighbours = new List<int>();
                }

                neighbours.Add(toId);
            }
            

        }

    }
}
