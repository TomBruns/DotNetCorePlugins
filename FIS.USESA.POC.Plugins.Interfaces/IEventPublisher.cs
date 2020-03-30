using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using FIS.USESA.POC.Plugins.Interfaces.Entities.Config;
using FIS.USESA.POC.Plugins.Interfaces.Entities.Kafka;

namespace FIS.USESA.POC.Plugins.Interfaces
{
    /// <summary>
    /// Interface IMessageSender
    /// </summary>
    /// <remarks>
    /// Define the interface that each plug-in will implement, each will be in a separate independent assy    
    /// </remarks>
    public interface IEventPublisher
    {
        void InjectConfig(KafkaServiceConfigBE kafkaConfig, ILogger logger);

        Task<PublishMsgResultsBE> PublishEvent(int eventId, string message);
    }
}
