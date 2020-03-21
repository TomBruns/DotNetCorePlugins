using System;
using System.Composition;

using FIS.USESA.POC.Plugins.Interfaces;
using FIS.USESA.POC.Plugins.Interfaces.Entities;

namespace FIS.USESA.POC.Plugins.ExampleB
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
    [ExportMetadata(MessageSenderType.ATTRIBUTE_NAME, @"EventTypeB")]
    public class EventTypeBPublisher : IEventPublisher
    {
        const string TOPIC_NAME = @"MESSAGE_TYPE_B_TOPIC";

        KafkaServiceConfigBE _configInfo;

        /// <summary>
        /// Injects the configuration.
        /// </summary>
        /// <param name="configInfo">The configuration information.</param>
        public void InjectConfig(KafkaServiceConfigBE configInfo)
        {
            _configInfo = configInfo;

            // TODO: configure ProducerConfig & SchemaRegistryConfig
        }

        /// <summary>
        /// Publishes the event.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>System.String.</returns>
        public string PublishEvent(string message)
        {
            string response = $"==> Publish Message: [{message}] from [{nameof(EventTypeBPublisher)}] to topic: [{TOPIC_NAME}] on kafka cluster: [{_configInfo.BootstrapServers}]";
            Console.WriteLine(response);

            // TODO: write publishing logic

            return response;
        }
    }
}