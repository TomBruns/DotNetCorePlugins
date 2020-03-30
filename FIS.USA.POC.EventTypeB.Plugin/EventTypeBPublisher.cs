using System;
using System.Composition;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using FIS.USA.POC.EventTypeB.Shared;
using FIS.USESA.POC.Plugins.Interfaces;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Config;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Kafka;
using FIS.USESA.POC.Plugins.Interfaces.Utilities;

namespace FIS.USA.POC.EventTypeB.Plugin
{
    /// <summary>
    /// This is a plug-in class and knows how to publish EventTypeB events
    /// </summary>
    [Export(typeof(IEventPublisher))]
    [ExportMetadata(MessageSenderType.ATTRIBUTE_NAME, @"EventTypeB")]
    public class EventTypeBPublisher : IEventPublisher
    {
        public static readonly string TOPIC_NAME = @"EVENT_TYPE_B_TOPIC";

        KafkaServiceConfigBE _kafkaConfig;
        ILogger _logger;
        Random _rnd = new Random();

        public EventTypeBPublisher()
        {
            // load plug-in specific configuration from appsettings.json file copied into the plug-in specific subfolder 
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json", false)
                .Build();

            // read config values (the plug-in would use this information to connect to the correct DB to gather addl data required 
            string dbConnString = configuration.GetConnectionString("DataConnection");
        }

        /// <summary>
        /// Injects the generic configuration known by the hosting process.
        /// </summary>
        /// <param name="kafkaConfig">The configuration information.</param>
        public void InjectConfig(KafkaServiceConfigBE kafkaConfig, ILogger logger)
        {
            _kafkaConfig = kafkaConfig;
            _logger = logger;
        }

        /// <summary>
        /// Publishes the event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message.</param>
        /// <returns>Task&lt;PublishMsgResultsBE&gt;.</returns>
        public async Task<PublishMsgResultsBE> PublishEvent(int eventId, string message)
        {
            // ====================================
            // Step 1: Go back to the DB to enrich the data about the event
            // ===================================
            // TODO:  To support this we probably need to inject more in the InjectConfig method (connection string, connectio object, ef db context)

            // ====================================
            // Step 2: Build the message key & body
            // ====================================
            var eventKey = new EventKey()
            {
                eventGuid = Guid.NewGuid().ToString()
            };

            var eventBody = new EventTypeBBody()
            {
                id = eventId,
                name = message,
                favorite_color = @"green",
                favorite_number = _rnd.Next(1, 100)
            };


            string errDetail = string.Empty;
            string errTitle = string.Empty;
            string errInstance = string.Empty;

            // ====================================
            // Step 3: Publish the Event
            // ====================================
            var eventPubResult = await KafkaUtilities.PublishMsgToTopic<EventKey, EventTypeBBody>(TOPIC_NAME, eventKey, eventBody, _kafkaConfig);

            if (!eventPubResult.IsSuccess)
            {
                errTitle = $"Error in {nameof(EventTypeBPublisher)}";
                errInstance = $"urn:myorganization:internalservererror:{Guid.NewGuid()}";
                errDetail = eventPubResult.Exception.ToString();

                _logger.LogError("Error Publishing Event [{@errTitle}], Details [{@errDetail}]", errTitle, errDetail);
            }
            else
            {
                _logger.LogInformation("Event [{@eventType}] published: eventKey [{eventKey}], eventBody [{eventBody}], AddlInfo: [{@addlInfo}]",
                                            nameof(EventTypeBPublisher),
                                            eventKey,
                                            eventBody,
                                            eventPubResult.Msg);
            }

            return eventPubResult;
        }
    }
}

