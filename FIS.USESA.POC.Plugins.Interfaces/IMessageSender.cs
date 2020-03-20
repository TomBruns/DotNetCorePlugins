using System;
using System.Collections.Generic;
using System.Text;

namespace FIS.USESA.POC.Plugins.Interfaces
{
    /// <summary>
    /// Interface IMessageSender
    /// </summary>
    /// <remarks>
    /// Define the interface that each plug-in will implement, each will be in a separate independent assy    
    /// </remarks>
    public interface IMessageSender
    {
        string Send(string message);
    }
}
