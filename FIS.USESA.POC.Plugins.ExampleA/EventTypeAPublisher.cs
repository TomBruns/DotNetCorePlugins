using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using FIS.USESA.POC.Plugins.ExampleADependency;
using FIS.USESA.POC.Plugins.Interfaces;
using FIS.USESA.POC.Plugins.Interfaces.Entities;

namespace FIS.USESA.POC.Plugins.ExampleA
{
    /// <summary>
    /// This is an example of a plug-in that can be dynamically loaded at runtime by a hosting process
    /// </summary>
    /// <remarks>
    /// A post build step for this project copies all the build outputs to a known (shared) location
    /// The Export attribute identifies this as a plug-in
    /// The ExportMetadata attribute allows this particular plug-in to be selected at runtime and must be unique!.
    /// </remarks>
    [Export(typeof(IEventPublisher))]
    [ExportMetadata(MessageSenderType.ATTRIBUTE_NAME, @"EventTypeA")]
    public class EventTypeAPublisher : IEventPublisher
    {
        const string TOPIC_NAME = @"MESSAGE_TYPE_A_TOPIC";

        KafkaServiceConfigBE _configInfo;
        private ProducerConfig _producerConfig;
        private SchemaRegistryConfig _schemaRegistryConfig;
        private ConsumerConfig _consumerConfig;
        private AvroSerializerConfig _avroSerializerConfig;

        /// <summary>
        /// Injects the configuration.
        /// </summary>
        /// <param name="configInfo">The configuration information.</param>
        public void InjectConfig(KafkaServiceConfigBE configInfo)
        {
            _configInfo = configInfo;

            _producerConfig = new ProducerConfig
            {
                BootstrapServers = _configInfo.BootstrapServers
            };

            _schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = _configInfo.SchemaRegistry,
                // optional schema registry client properties:
                RequestTimeoutMs = 5000,
                MaxCachedSchemas = 10
            };

            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configInfo.BootstrapServers,
                GroupId = "avro-specific-example-group"
            };

            _avroSerializerConfig = new AvroSerializerConfig
            {
                // optional Avro serializer properties:
                BufferBytes = 100,
                AutoRegisterSchemas = true
            };
        }

        /// <summary>
        /// Publishes the event.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>System.String.</returns>
        public async Task<string> PublishEvent(string message)
        {
            string testDependency = ExampleAChildDependency.Test();
            string response = $"==> Publish Message: [{message}] from [{nameof(EventTypeAPublisher)}] and [{testDependency}] to topic: [{TOPIC_NAME}] on kafka cluster: [{_configInfo.BootstrapServers}]";
            Console.WriteLine(response);

            await Produce();

            return response;
        }

        private async Task Produce()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var consumeTask = Task.Run(() =>
            {
                using (var schemaRegistry = new CachedSchemaRegistryClient(_schemaRegistryConfig))
                using (var consumer =
                    new ConsumerBuilder<string, User.User>(_consumerConfig)
                        .SetKeyDeserializer(new AvroDeserializer<string>(schemaRegistry).AsSyncOverAsync())
                        .SetValueDeserializer(new AvroDeserializer<User.User>(schemaRegistry).AsSyncOverAsync())
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build())
                {
                    consumer.Subscribe(TOPIC_NAME);

                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);

                                Console.WriteLine($"user name: {consumeResult.Message.Key}, favorite color: {consumeResult.Value.favorite_color}");
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Consume error: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        consumer.Close();
                    }
                }
            });

            using (var schemaRegistry = new CachedSchemaRegistryClient(_schemaRegistryConfig))
            using (var producer =
                new ProducerBuilder<string, User.User>(_producerConfig)
                    .SetKeySerializer(new AvroSerializer<string>(schemaRegistry))
                    .SetValueSerializer(new AvroSerializer<User.User>(schemaRegistry))
                    .Build())
            {
                Console.WriteLine($"{producer.Name} producing on {TOPIC_NAME}. Enter user names, q to exit.");

                int i = 0;
                string text;
                while ((text = Console.ReadLine()) != "q")
                {
                    User.User user = new User.User { name = text, favorite_color = "green", favorite_number = i++ };
                    await producer
                        .ProduceAsync(TOPIC_NAME, new Message<string, User.User> { Key = text, Value = user })
                        .ContinueWith(task => task.IsFaulted
                            ? $"error producing message: {task.Exception.Message}"
                            : $"produced to: {task.Result.TopicPartitionOffset}");
                }
            }

            cts.Cancel();
        }
    }
}
