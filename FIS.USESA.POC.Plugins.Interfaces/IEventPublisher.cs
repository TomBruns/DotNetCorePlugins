using System;
using System.Collections.Generic;
using System.Text;

using FIS.USESA.POC.Plugins.Interfaces.Entities;

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
        void InjectConfig(KafkaServiceConfigBE configInfo);

        string PublishEvent(string message);
    }
}
