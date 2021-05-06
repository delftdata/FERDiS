using BlackSP.Benchmarks.Kafka;
using BlackSP.Benchmarks.Graph.Models;
using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Benchmarks.Graph
{
    /// <summary>
    /// Produces Pagerank data from a provided file containing edge (int, int) pairs
    /// </summary>
    public class Producer
    {

        /// <summary>
        /// The edge file is expected to be formatted line-by-line as "int\tint"<br/>
        /// The first int is the "from" pageId and the second int is the "to" pageId
        /// https://github.com/commoncrawl/cc-crawl-statistics
        /// </summary>
        /// <param name="brokerList"></param>
        /// <param name="edgeFileLocation"></param>
        /// <returns></returns>
        public static async Task StartProductingGraphData(string edgeFileLocation)
        {
            _ = edgeFileLocation ?? throw new ArgumentNullException(nameof(edgeFileLocation));

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaUtils.GetKafkaBrokerString(),
                Partitioner = Partitioner.Consistent
            };

            
            using var adjacencyProducer = new ProducerBuilder<int, Adjacency>(config)
                .SetValueSerializer(new ProtoBufAsyncValueSerializer<Adjacency>())
                .SetErrorHandler((prod, err) => Console.WriteLine($"Adjacency stream error: {err}"))
                .Build();
            
            using StreamReader reader = File.OpenText(edgeFileLocation);

            int i = 0;
            string row = string.Empty;
            int currentFromId = 0;
            List<int> neighbours = new List<int>();
            Console.WriteLine("Starting producing Adjacency events to Kafka");
            while ((row = reader.ReadLine()) != null)
            {
                var ids = row.Split('\t');
                var fromId = int.Parse(ids[0]);
                var toId = int.Parse(ids[1]);

                if(fromId != currentFromId)
                {
                    /*await*/ adjacencyProducer.ProduceAdjacency(currentFromId, neighbours.ToArray());
                    var lastFromId = currentFromId;
                    currentFromId = fromId;
                    neighbours = new List<int>();

                    while(lastFromId+1 < currentFromId) //need to insert empty adjacencies for page ids which have no neighbours
                    {
                        lastFromId++;
                        /*await*/ adjacencyProducer.ProduceAdjacency(lastFromId, neighbours.ToArray());
                    }
                }
                neighbours.Add(toId);
                i++;
                if(i % 500 == 0)
                {
                    Console.WriteLine($"Producing progress @ page {currentFromId}");
                }
            }
            adjacencyProducer.Flush();
            Console.WriteLine("Completed producing Adjacency events to Kafka");

        }

    }

    public static class ProducerExtensions
    {
        public static async Task ProduceAdjacency(this IProducer<int, Adjacency> producer, int pageId, int[] neighbours)
        {
            var adjacency = new Adjacency
            {
                PageId = pageId,
                Neighbours = neighbours
            };

            var message = new Message<int, Adjacency>
            {
                Key = adjacency.PageId,
                Value = adjacency
            };
            await producer.ProduceAsync(Adjacency.KafkaTopicName, message);
        }
    }
}
