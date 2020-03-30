using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Serilog;
using Serilog.Extensions.Logging;

using FIS.USA.POC.EventTypeB.Plugin;
using FIS.USA.POC.EventTypeB.Shared;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Config;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Kafka;

namespace FIS.USA.POC.EventTypeB.Consumer
{
    /// <summary>
    /// This is an example topic consumer.
    /// </summary>
    class Program
    {
        private const string CONSUMER_GROUP_NAME = @"EVENT_TYPE_B_CONSUMER_GROUP";

        private KafkaServiceConfigBE _kafkaConfig;

        private ProducerConfig _producerConfig;
        private SchemaRegistryConfig _schemaRegistryConfig;
        private ConsumerConfig _consumerConfig;
        private AvroSerializerConfig _avroSerializerConfig;

        private Microsoft.Extensions.Logging.ILogger _logger;

        static void Main(string[] args)
        {
            // configure logging
            Log.Logger = new LoggerConfiguration()
                                .WriteTo.Console()
                                .CreateLogger();


            Program program = new Program();
            program.Config();

            CancellationToken stoppingToken = new CancellationToken();
            program.ProcessTopicMessages(stoppingToken);
        }

        internal void Config()
        {
            _logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(Program));

            // load consumer specific configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // get the kafka connection information
            _kafkaConfig = configuration.GetSection("kafkaConfig").Get<KafkaServiceConfigBE>();

            _producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers
            };

            _schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = _kafkaConfig.SchemaRegistry,
                // optional schema registry client properties:
                RequestTimeoutMs = 5000,
                MaxCachedSchemas = 10
            };

            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                GroupId = CONSUMER_GROUP_NAME
            };

            _avroSerializerConfig = new AvroSerializerConfig
            {
                // optional Avro serializer properties:
                BufferBytes = 100,
                AutoRegisterSchemas = true
            };
        }

        internal void ProcessTopicMessages(CancellationToken stoppingToken)
        {
            string topicName = EventTypeBPublisher.TOPIC_NAME;

            using var schemaRegistry = new CachedSchemaRegistryClient(_schemaRegistryConfig);
            using var consumer = new ConsumerBuilder<EventKey, EventTypeBBody>(_consumerConfig)
                                        .SetKeyDeserializer(new AvroDeserializer<EventKey>(schemaRegistry).AsSyncOverAsync())
                                        .SetValueDeserializer(new AvroDeserializer<EventTypeBBody>(schemaRegistry).AsSyncOverAsync())
                                        .SetErrorHandler((_, e) => _logger.LogError($"Consume Error in {Process.GetCurrentProcess().ProcessName}: {e.Reason}"))
                                        .Build();

            consumer.Subscribe(topicName);

            _logger.LogInformation("KafkaWorker processing...Subscribed to Topic: [{@TopicName}]", topicName);

            try
            {
                while (true)
                {
                    try
                    {
                        // poll and block until a consumable message is available
                        var consumeResult = consumer.Consume(stoppingToken);

                        var eventKey = consumeResult.Key;
                        var eventData = consumeResult.Value;

                        _logger.LogInformation("KafkaWorker processing.. Topic: [{@TopicName}], Offset: [{PartitionOffset}], EventKey: [{eventKey}], EventData: [{eventData}]",
                                                consumeResult.Topic,
                                                consumeResult.TopicPartitionOffset.Offset.Value,
                                                eventKey.eventGuid,
                                                eventData.ToString());

                        // TODO: Implement event handling logic

                        // commits the Topic read offset
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError(e, $"KafkaWorker error consuming.. [{e.Error.Reason}] Msg: [{e.Message}] [{@e}]");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"KafkaWorker general error.. [{@ex}]");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }
    }
}
