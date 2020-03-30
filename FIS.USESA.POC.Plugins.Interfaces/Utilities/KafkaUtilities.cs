using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

using FIS.USESA.POC.Plugins.Interfaces.Entities.Config;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Kafka;

namespace FIS.USESA.POC.Plugins.Interfaces.Utilities
{
    /// <summary>
    /// This class contains helper methods supporting publishing msgs to a Kafka Topic
    /// </summary>
    public static class KafkaUtilities
    {
        public static async Task<PublishMsgResultsBE> PublishMsgToTopic<K, V>(string topicName, K key, V value, KafkaServiceConfigBE kafkaConfig)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaConfig.BootstrapServers
            };

            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                // Note: you can specify more than one schema registry url using the
                // schema.registry.url property for redundancy (comma separated list). 
                // The property name is not plural to follow the convention set by
                // the Java implementation.
                Url = kafkaConfig.SchemaRegistry,
                // optional schema registry client properties:
                RequestTimeoutMs = 5000,
                MaxCachedSchemas = 10
            };

            return await PublishMsgToTopic<K, V>(topicName, key, value, producerConfig, schemaRegistryConfig);
        }

        public static async Task<PublishMsgResultsBE> PublishMsgToTopic<K, V>(string topicName, K key, V value, ProducerConfig producerConfig, SchemaRegistryConfig schemaRegistryConfig)
        {
            using var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);
            using var producer = new ProducerBuilder<K, V>(producerConfig)
                                    .SetKeySerializer(new AvroSerializer<K>(schemaRegistry))
                                    .SetValueSerializer(new AvroSerializer<V>(schemaRegistry))
                                    .Build();

            PublishMsgResultsBE publishMsgResults = await producer.ProduceAsync(topicName, new Message<K, V> { Key = key, Value = value })
                                    .ContinueWith<PublishMsgResultsBE>(task => task.IsFaulted
                                    ? new PublishMsgResultsBE() { IsSuccess = false, Msg = $"error producing message: {task.Exception.Message}", Exception = task.Exception }
                                    : new PublishMsgResultsBE() { IsSuccess = true, Msg = $"produced to: {task.Result.TopicPartitionOffset} " });

            publishMsgResults.Name = producer.Name;
            publishMsgResults.TopicName = topicName;

            return publishMsgResults;
        }
    }
}
