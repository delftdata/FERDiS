using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Kafka
{
    public static class KafkaUtils
    {

        /// <summary>
        /// Attempts to fetch the brokers from environment variables: KAFKA_BROKER_DNS_TEMPLATE and KAFKA_BROKER_COUNT
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetKafkaBrokers()
        {
            var dnsTemplate = Environment.GetEnvironmentVariable("KAFKA_BROKER_DNS_TEMPLATE");
            var brokerCount = int.Parse(Environment.GetEnvironmentVariable("KAFKA_BROKER_COUNT"));

            for (int i = 0; i < brokerCount; i++)
            {
                yield return string.Format(dnsTemplate, i);
            }
        }

        /// <summary>
        /// Concatenated results of GetKafkaBrokers in comma separated format (just how Kafka likes it)
        /// </summary>
        /// <returns></returns>
        public static string GetKafkaBrokerString()
        {
            return string.Join(",", GetKafkaBrokers());
        }

    }
}
